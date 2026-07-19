using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Midas.Api.Services;

public class JwtService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwt, ApplicationDbContext context, IEmailSender emailSender) : IJwtService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly ApplicationDbContext _context = context;
	private readonly IEmailSender _emailSender = emailSender;
	private readonly JwtSettings _jwt = jwt.Value;

	public async Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user)
	{
		var userClaims = await _userManager.GetClaimsAsync(user);
		var roles = await _userManager.GetRolesAsync(user);
		var roleClaims = new List<Claim>();
		foreach (var role in roles)
		{
			roleClaims.Add(new Claim(ClaimTypes.Role, role));
		}
		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Email, user.Email!),
			new Claim("uid", user.Id)
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
		return jwtSecurityToken;
	}

	public byte[] GenerateRefreshToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		// var token = new RefreshToken
		// {
		// 	Token = Convert.ToBase64String(bytes),
		// 	TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
		// 	ExpiresAt = DateTime.UtcNow.AddDays(30)
		// };
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
		var oldRefreshToken = await _context.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash);
		if (oldRefreshToken is null)
		{
			AuthResult.Succeeded = false;
			AuthResult.Errors = ["Invalid token."];
			return AuthResult;
		}
		ApplicationUser user = oldRefreshToken.User!;
		if (oldRefreshToken.IsExpired)
		{
			AuthResult.Succeeded = false;
			AuthResult.Errors.Add("Expired token.");
		}
		var rowsAffected = await _context.RefreshTokens
			.Where(t => t.Id == oldRefreshToken.Id && t.RevokedAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
		if (rowsAffected == 0)
		{
			await _context.RefreshTokens
				.Where(t => t.ApplicationUserId == oldRefreshToken.ApplicationUserId &&
							t.RevokedAt == null)
				.ExecuteUpdateAsync(setters =>
					setters.SetProperty(
						t => t.RevokedAt,
						DateTime.UtcNow));
			await _emailSender.SendSecurityAlertAsync(user, user.Email!);
			AuthResult.Succeeded = false;
			AuthResult.Errors.Add("Revoked token.");
			return AuthResult;
		}
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
		_context.RefreshTokens.Add(newToken);
		await _context.SaveChangesAsync();
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
