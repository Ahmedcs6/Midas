using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
namespace Midas.Api.Services;

public class AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwt, ApplicationDbContext context, IEmailSender emailSender) : IAuthService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly ApplicationDbContext _context = context;
	private readonly IEmailSender _emailSender = emailSender;
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
			Succeeded = true,
			User = user
		};
	}
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

		var token = await CreateJwtTokenAsync(user);
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
			await _emailSender.SendSecurityAlertAsync(oldRefreshToken.User, oldRefreshToken.User.Email);
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
		var AccessToken = await CreateJwtTokenAsync(oldRefreshToken.User);
		AuthResult.User = oldRefreshToken.User;
		AuthResult.AccessToken = new JwtSecurityTokenHandler().WriteToken(AccessToken);
		AuthResult.ExpiresOn = AccessToken.ValidTo;
		AuthResult.RefreshToken = newToken;
		return AuthResult;
	}
	public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			return new()
			{
				Succeeded = true
			};
		}
		if (!await _userManager.IsEmailConfirmedAsync(user))
			return new()
			{
				Succeeded = false,
				Errors = ["Please confirm your Email."]
			};
		var token =
			await _userManager.GeneratePasswordResetTokenAsync(user);

		var encodedToken =
			WebEncoders.Base64UrlEncode(
				Encoding.UTF8.GetBytes(token));

		var resetLink = $"https://localhost:7103/reset-password?userId={user.Id}&token={encodedToken}";
		await _emailSender.SendPasswordResetLinkAsync(user, model.Email, resetLink);
		return new()
		{
			Succeeded = true
		};
	}

	public async Task SendConfirmEmailAsync(ConfirmEmailDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null || await _userManager.IsEmailConfirmedAsync(user))
		{
			return;
		}
		string token =
			await _userManager.GenerateEmailConfirmationTokenAsync(user);
		string encodedToken =
	WebEncoders.Base64UrlEncode(
		Encoding.UTF8.GetBytes(token));

		string confirmationLink =
			$"https://localhost:7103/confirm-email?userId={user.Id}&token={encodedToken}";
		await _emailSender.SendConfirmationLinkAsync(user, model.Email, confirmationLink);
	}
	public async Task<AuthResult> ConfirmEmailAsync(string userId, string token)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
		{
			return new()
			{
				Succeeded = false,
				Errors = ["user not found."]
			};
		}
		token = Encoding.UTF8.GetString(
			WebEncoders.Base64UrlDecode(token));

		var result =
			await _userManager.ConfirmEmailAsync(user, token);

		if (!result.Succeeded)
		{
			return new()
			{
				Succeeded = false,
				Errors = [.. result.Errors.Select(e => e.Description)]
			};
		}
		return new()
		{
			Succeeded = true
		};
	}
	public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto model)
	{
		var user = await _userManager.FindByIdAsync(model.Id);
		if (user is null)
		{
			return new()
			{
				Succeeded = false,
				Errors = ["user not found."]
			};
		}
		var token = Encoding.UTF8.GetString(
			WebEncoders.Base64UrlDecode(model.Token));
		var result =
			await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
		if (!result.Succeeded)
		{
			return new()
			{
				Succeeded = false,
				Errors = [.. result.Errors.Select(e => e.Description)]
			};
		}
		return new()
		{
			Succeeded = true
		};
	}
}
