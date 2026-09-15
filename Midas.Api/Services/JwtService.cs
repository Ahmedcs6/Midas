using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Midas.Api.Services;

public class JwtService(ILogger<JwtService> logger, Channel<IEmailJob> channel, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwt, ApplicationDbContext context) : IJwtService
{
	private readonly JwtSettings _jwt = jwt.Value;

	public async Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user, Guid sessionId)
	{
		logger.LogDebug("Creating JWT for user {UserId}, session {SessionId}", user.Id, sessionId);

		var userClaims = await userManager.GetClaimsAsync(user);
		var roles = await userManager.GetRolesAsync(user);

		var roleClaims = roles.Select(role =>
				new Claim(ClaimTypes.Role, role));

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Email, user.Email!),
			new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
			new Claim("session_id", sessionId.ToString())
		}
		.Union(userClaims)
			.Union(roleClaims);

		var symmetricSecurityKey =
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

		var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

		var jwtSecurityToken = new JwtSecurityToken(
				issuer: _jwt.Issuer,
				audience: _jwt.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes),
				signingCredentials: signingCredentials);

		logger.LogDebug("JWT created for {UserId}, Session={SessionId}, Jti={Jti}, Expires={Expires}, Roles={Roles}", user.Id, sessionId, jwtSecurityToken.Id, jwtSecurityToken.ValidTo, string.Join(", ", roles));

		return jwtSecurityToken;
	}
	public byte[] GenerateRefreshToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		logger.LogTrace("Generated refresh token bytes");
		return bytes;
	}
	public async Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest model)
	{
		byte[] bytes;
		try
		{
			bytes = Convert.FromBase64String(model.RefreshToken);
		}
		catch (FormatException)
		{
			logger.LogWarning("Refresh failed: malformed token");
			return new()
			{
				Success = false,
				Error = Error.Validation,
				Message = "Invalid token."
			};
		}
		var hash = Convert.ToBase64String(SHA256.HashData(bytes));

		var oldRefreshToken = await context.RefreshTokens
			.Include(t => t.Session)
			.ThenInclude(s => s.User)
			.FirstOrDefaultAsync(t => t.TokenHash == hash);

		logger.LogDebug(
				"Refresh attempt: token hash prefix {HashPrefix}",
				hash[..8]);

		if (oldRefreshToken is null)
		{
			logger.LogWarning("Refresh failed: token hash not found");

			return new()
			{
				Success = false,
				Error = Error.Validation,
				Message = "Invalid token."
			};
		}

		var user = oldRefreshToken.Session.User;

		if (oldRefreshToken.IsExpired)
		{
			logger.LogWarning(
					"Refresh failed: expired token for {UserId}, expired at {ExpiredAt}",
					user.Id,
					oldRefreshToken.ExpiresAt);

			return new()
			{
				Success = false,
				Error = Error.Validation,
				Message = "Expired token."
			};
		}

		var rowsAffected = await context.RefreshTokens
			.Where(t =>
					t.Id == oldRefreshToken.Id &&
					t.RevokedAt == null)
			.ExecuteUpdateAsync(setters => setters
					.SetProperty(t => t.RevokedAt, DateTime.UtcNow));

		if (rowsAffected == 0)
		{
			logger.LogError(
					"SECURITY ALERT: Token reuse detected for {UserId}. Token {TokenId} was already revoked.",
					user.Id,
					oldRefreshToken.Id);

			await context.RefreshTokens
				.Where(t =>
						t.SessionId == oldRefreshToken.SessionId &&
						t.RevokedAt == null)
				.ExecuteUpdateAsync(setters => setters
						.SetProperty(t => t.RevokedAt, DateTime.UtcNow));

			logger.LogInformation(
					"Revoked all active tokens for session {SessionId} due to suspected reuse",
					oldRefreshToken.SessionId);

			await channel.Writer.WriteAsync(
					new SecurityAlertJob(user, user.Email!));

			return new()
			{
				Success = false,
				Error = Error.AuthenticationRequired,
				Message = "Revoked token."
			};
		}

		logger.LogDebug(
				"Revoked old refresh token {TokenId} for {UserId}",
				oldRefreshToken.Id,
				user.Id);

		bytes = GenerateRefreshToken();

		var newToken = new RefreshToken
		{
			SessionId = oldRefreshToken.SessionId,
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30)
		};

		context.RefreshTokens.Add(newToken);

		await context.SaveChangesAsync();

		var accessToken = await CreateJwtTokenAsync(user, newToken.SessionId);

		logger.LogInformation(
				"Token refreshed for {UserId}: new token {TokenId}, expires {ExpiresAt}",
				user.Id,
				newToken.Id,
				newToken.ExpiresAt);

		return new()
		{
			Success = true,
			Data = new()
			{
				AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
				AccessTokenExpiresAt = accessToken.ValidTo,
				RefreshToken = Convert.ToBase64String(bytes),
				RefreshTokenExpiresAt = newToken.ExpiresAt
			}
		};
	}
}
