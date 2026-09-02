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

	public async Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user)
	{

		logger.LogDebug("Creating JWT for user {UserId}", user.Id);
		var userClaims = await userManager.GetClaimsAsync(user);
		var roles = await userManager.GetRolesAsync(user);
		var roleClaims = new List<Claim>();
		foreach (var role in roles)
		{
			roleClaims.Add(new Claim(ClaimTypes.Role, role));
		}
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Email, user.Email!),
			new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!)
		}
		.Union(userClaims)
		.Union(roleClaims);
		var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
		var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

		var jwtSecurityToken = new JwtSecurityToken(
				issuer: _jwt.Issuer,
				audience: _jwt.Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenLifetimeMinutes),
				signingCredentials: signingCredentials
				);

		logger.LogDebug(
				   "JWT created for {UserId}: Jti={Jti}, Expires={Expires}, Roles={Roles}",
				   user.Id,
				   jwtSecurityToken.Id,
				   jwtSecurityToken.ValidTo,
				   string.Join(", ", roles));
		return jwtSecurityToken;
	}

	public byte[] GenerateRefreshToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		logger.LogTrace("Generated refresh token bytes");
		return bytes;
	}

	public async Task<AuthResult> RefreshAsync(RefreshTokenRequest model)
	{
		var AuthResult = new AuthResult
		{
			Succeeded = true
		};
		var bytes = Convert.FromBase64String(model.RefreshToken);
		var hash = Convert.ToBase64String(SHA256.HashData(bytes));
		var oldRefreshToken = await context.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash);

		logger.LogDebug("Refresh attempt: token hash prefix {HashPrefix}", hash[..8]);
		if (oldRefreshToken is null)
		{
			logger.LogWarning("Refresh failed: token hash not found");
			AuthResult.Succeeded = false;
			AuthResult.Errors = ["Invalid token."];
			return AuthResult;
		}
		ApplicationUser user = oldRefreshToken.User!;
		if (oldRefreshToken.IsExpired)
		{
			logger.LogWarning(
							"Refresh failed: expired token for {UserId}, expired at {ExpiredAt}",
							user.Id,
							oldRefreshToken.ExpiresAt);
			AuthResult.Succeeded = false;
			AuthResult.Errors.Add("Expired token.");
		}
		var rowsAffected = await context.RefreshTokens
			.Where(t => t.Id == oldRefreshToken.Id && t.RevokedAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
		if (rowsAffected == 0)
		{
			logger.LogError(
							"SECURITY ALERT: Token reuse detected for {UserId}. Token {TokenId} was already revoked.",
							user.Id,
							oldRefreshToken.Id);
			await context.RefreshTokens
				.Where(t => t.ApplicationUserId == oldRefreshToken.ApplicationUserId &&
							t.RevokedAt == null)
				.ExecuteUpdateAsync(setters =>
					setters.SetProperty(
						t => t.RevokedAt,
						DateTime.UtcNow));
			logger.LogInformation("Revoked all active tokens for {UserId} due to suspected reuse", user.Id);
			await channel.Writer.WriteAsync(new SecurityAlertJob(user, user.Email!));
			AuthResult.Succeeded = false;
			AuthResult.Errors.Add("Revoked token.");
			return AuthResult;
		}
		logger.LogDebug("Revoked old refresh token {TokenId} for {UserId}", oldRefreshToken.Id, user.Id);
		if (!AuthResult.Succeeded)
			return AuthResult;
		bytes = GenerateRefreshToken();
		RefreshToken newToken = new()
		{
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30),
			Client = oldRefreshToken.Client,
			ApplicationUserId = oldRefreshToken.ApplicationUserId
		};
		context.RefreshTokens.Add(newToken);
		await context.SaveChangesAsync();
		logger.LogInformation(
				  "Token refreshed for {UserId}: new token {TokenId}, expires {ExpiresAt}",
				  user.Id,
				  newToken.Id,
				  newToken.ExpiresAt);
		var AccessToken = await CreateJwtTokenAsync(user);
		AuthResult.User = new()
		{
			FirstName = user.FirstName,
			LastName = user.LastName,
			UserName = user.UserName!,
			Gender = user.Gender
		};
		AuthResult.RefreshTokenResponse = new()
		{
			AccessToken = new JwtSecurityTokenHandler().WriteToken(AccessToken),
			AccessTokenExpiresAt = AccessToken.ValidTo,
			RefreshToken = Convert.ToBase64String(bytes),
			RefreshTokenExpiresAt = newToken.ExpiresAt
		};
		return AuthResult;
	}
}
