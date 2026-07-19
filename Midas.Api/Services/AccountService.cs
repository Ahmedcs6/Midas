using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Midas.Api.Services;

public class AccountService(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IEmailSender emailSender, IJwtService jwtService) : IAccountService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly ApplicationDbContext _context = context;
	private readonly IEmailSender _emailSender = emailSender;
	private readonly IJwtService _jwtService = jwtService;

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
			User = new()
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				UserName = user.UserName,
				Gender = user.Gender
			}
		};
	}
	public async Task<AuthResult> LoginAsync(LoginDto model)
	{
		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
		{
			return new AuthResult { Succeeded = false, Errors = ["Please confirm your email."] };
		}

		if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
		{
			return new AuthResult { Succeeded = false, Errors = ["Invalid email or password."] };
		}
		await _context.RefreshTokens
						.Where(t => t.ApplicationUserId == user.Id && t.Client == model.Client &&
									t.RevokedAt == null)
						.ExecuteUpdateAsync(setters =>
							setters.SetProperty(
								t => t.RevokedAt,
								DateTime.UtcNow));

		var token = await _jwtService.CreateJwtTokenAsync(user);
		var bytes = _jwtService.GenerateRefreshToken();
		var refreshToken = new RefreshToken
		{
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30),
			Client = model.Client,
			ApplicationUserId = user.Id
		};
		_context.RefreshTokens.Add(refreshToken);
		await _context.SaveChangesAsync();
		return new()
		{
			Succeeded = true,
			User = new()
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				UserName = user.UserName!,
				Gender = user.Gender
			},
			RefreshTokenResponse = new()
			{
				AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
				AccessTokenExpiresAt = token.ValidTo,
				RefreshToken = Convert.ToBase64String(bytes),
				RefreshTokenExpiresAt = refreshToken.ExpiresAt
			}
		};
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
