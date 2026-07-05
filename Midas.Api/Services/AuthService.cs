using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
namespace Midas.Api.Services;

public class AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwt, ApplicationDbContext context, IIdentityEmailService identityEmailService) : IAuthService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly ApplicationDbContext _context = context;
	private readonly IIdentityEmailService _identityEmailService = identityEmailService;
	private readonly JwtSettings _jwt = jwt.Value;


	public async Task<AuthResult> RegisterAsync(RegisterDto model)
	{
		ApplicationUser user = new()
		{
			FirstName = model.FirstName,
			LastName = model.LastName,
			UserName = model.UserName,
			Gender = model.Gender,
			Email = model.Email
		};
		var result = await _userManager.CreateAsync(user, model.Password);
		if (!result.Succeeded)
		{
			return new AuthResult { Succeeded = false, Errors = [.. result.Errors.Select(e => e.Description)] };
		}
		result = await _userManager.AddToRoleAsync(user, "User");
		if (!result.Succeeded)
		{
			return new AuthResult { Succeeded = false, Errors = [.. result.Errors.Select(e => e.Description)] };
		}
		return new()
		{
			Succeeded = true
		};
	}
	public async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
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
	public async Task<AuthResult> LoginAsync(LoginDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
		{
			return new AuthResult { Succeeded = false, Errors = ["Invalid email or password."] };
		}

		if (!await _userManager.IsEmailConfirmedAsync(user))
		{
			return new AuthResult { Succeeded = false, Errors = ["Please confirm your email."] };
		}
		await _context.RefreshTokens
						.Where(t => t.ApplicationUserId == user.Id && t.Client == model.Client &&
									t.RevokedAt == null)
						.ExecuteUpdateAsync(setters =>
							setters.SetProperty(
								t => t.RevokedAt,
								DateTime.UtcNow));

		var token = await CreateJwtToken(user);
		var refreshToken = GenerateRefreshToken();
		refreshToken.Client = model.Client;
		refreshToken.ApplicationUserId = user.Id;
		_context.RefreshTokens.Add(refreshToken);
		await _context.SaveChangesAsync();
		return new()
		{
			Succeeded = true,
			User = user,
			AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
			ExpiresOn = token.ValidTo,
			RefreshToken = refreshToken
		};
	}
	public RefreshToken GenerateRefreshToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		var token = new RefreshToken
		{
			Token = Convert.ToBase64String(bytes),
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30)
		};
		return token;
	}
	public async Task<AuthResult> Refresh(RefreshTokenRequest model)
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
			await _identityEmailService.SecurityAlert(oldRefreshToken.User.Email);
			AuthResult.Succeeded = false;
			AuthResult.Errors.Add("Revoked token.");
			return AuthResult;
		}
		if (!AuthResult.Succeeded)
			return AuthResult;
		RefreshToken newToken = GenerateRefreshToken();
		newToken.Client = oldRefreshToken.Client;
		newToken.ApplicationUserId = oldRefreshToken.ApplicationUserId;
		_context.RefreshTokens.Add(newToken);
		await _context.SaveChangesAsync();
		var AccessToken = await CreateJwtToken(oldRefreshToken.User);
		AuthResult.User = oldRefreshToken.User;
		AuthResult.AccessToken = new JwtSecurityTokenHandler().WriteToken(AccessToken);
		AuthResult.ExpiresOn = AccessToken.ValidTo;
		AuthResult.RefreshToken = newToken;
		return AuthResult;
	}
}
