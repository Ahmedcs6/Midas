using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.AspNetCore.WebUtilities;

namespace Midas.Api.Services;

public class AccountService(ILogger<AccountService> logger, Channel<IEmailJob> channel, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IJwtService jwtService) : IAccountService
{

	public async Task<AuthResult> RegisterAsync(RegisterRequest model)
	{
		await using var transaction =
			await context.Database.BeginTransactionAsync();
		ApplicationUser user = new()
		{
			FirstName = model.FirstName,
			LastName = model.LastName,
			UserName = model.UserName,
			Gender = model.Gender,
			Email = model.Email
		};
		var result = await userManager.CreateAsync(user, model.Password);

		if (!result.Succeeded)
		{
			logger.LogWarning(
							"Registration failed for {Email}. Errors: {Errors}",
							model.Email,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			return new AuthResult { Succeeded = false, Errors = [.. result.Errors.Select(e => e.Description)] };
		}
		result = await userManager.AddToRoleAsync(user, "User");
		if (!result.Succeeded)
		{
			logger.LogError(
						  "Failed to assign 'User' role to {UserId}. Errors: {Errors}",
						  user.Id,
						  string.Join(", ", result.Errors.Select(e => e.Description)));
			return new AuthResult { Succeeded = false, Errors = [.. result.Errors.Select(e => e.Description)] };
		}
		await transaction.CommitAsync();
		logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);
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
	public async Task<AuthResult> LoginAsync(LoginRequest model)
	{
		var user = await userManager.FindByEmailAsync(model.Email);
		if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogWarning("Login blocked: email not confirmed for {Email}", model.Email);
			return new AuthResult { Succeeded = false, Errors = ["Please confirm your email."] };
		}

		if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
		{
			logger.LogWarning("Failed login attempt for {Email}", model.Email);
			return new AuthResult { Succeeded = false, Errors = ["Invalid email or password."] };
		}
		logger.LogInformation("User logged in: {UserId} ({Email}) from client {Client}", user.Id, user.Email, model.Client);
		await context.RefreshTokens
						.Where(t => t.ApplicationUserId == user.Id && t.Client == model.Client &&
									t.RevokedAt == null)
						.ExecuteUpdateAsync(setters =>
							setters.SetProperty(
								t => t.RevokedAt,
								DateTime.UtcNow));
		logger.LogDebug("Revoked previous refresh tokens for {UserId} on client {Client}", user.Id, model.Client);
		var token = await jwtService.CreateJwtTokenAsync(user);
		var bytes = jwtService.GenerateRefreshToken();
		var refreshToken = new RefreshToken
		{
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30),
			Client = model.Client,
			ApplicationUserId = user.Id
		};
		context.RefreshTokens.Add(refreshToken);
		await context.SaveChangesAsync();
		logger.LogInformation("Issued new refresh token for {UserId}, expires {ExpiresAt:O}", user.Id, refreshToken.ExpiresAt);
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
	public async Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest model)
	{
		var user = await userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			logger.LogInformation("Password reset requested for non-existent email: {Email}", model.Email);
			return new()
			{
				Succeeded = true
			};
		}
		if (!await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogWarning("Password reset blocked: email not confirmed for {UserId}", user.Id);
			return new()
			{
				Succeeded = false,
				Errors = ["Please confirm your Email."]
			};
		}
		var token =
			await userManager.GeneratePasswordResetTokenAsync(user);

		var encodedToken =
			WebEncoders.Base64UrlEncode(
				Encoding.UTF8.GetBytes(token));

		var resetLink = $"https://localhost:7103/reset-password?userId={user.Id}&token={encodedToken}";
		logger.LogInformation("Password reset link generated for {UserId}", user.Id);
		await channel.Writer.WriteAsync(new PasswordResetJob(user, model.Email, resetLink));
		return new()
		{
			Succeeded = true
		};
	}
	public async Task SendConfirmEmailAsync(ConfirmEmailRequset model)
	{
		var user = await userManager.FindByEmailAsync(model.Email);
		if (user is null || await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogDebug("Confirmation email skipped for {Email}: user not found or already confirmed", model.Email);
			return;
		}
		string token =
			await userManager.GenerateEmailConfirmationTokenAsync(user);
		string encodedToken =
	WebEncoders.Base64UrlEncode(
		Encoding.UTF8.GetBytes(token));

		string confirmationLink =
			$"https://localhost:7103/confirm-email?userId={user.Id}&token={encodedToken}";
		logger.LogInformation("Sending confirmation email to {UserId} ({Email})", user.Id, user.Email);
		await channel.Writer.WriteAsync(new ConfirmEmailJob(user, model.Email, confirmationLink));
	}
	public async Task<AuthResult> ConfirmEmailAsync(Guid userId, string token)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
		{
			logger.LogWarning("Email confirmation failed: user {UserId} not found", userId);
			return new()
			{
				Succeeded = false,
				Errors = ["user not found."]
			};
		}
		token = Encoding.UTF8.GetString(
			WebEncoders.Base64UrlDecode(token));

		var result =
			await userManager.ConfirmEmailAsync(user, token);

		if (!result.Succeeded)
		{
			logger.LogError(
							"Email confirmation failed for {UserId}. Errors: {Errors}",
							userId,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			return new()
			{
				Succeeded = false,
				Errors = [.. result.Errors.Select(e => e.Description)]
			};
		}
		logger.LogInformation("Email confirmed for {UserId} ({Email})", user.Id, user.Email);
		return new()
		{
			Succeeded = true
		};
	}
	public async Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest model)
	{
		var user = await userManager.FindByIdAsync(model.Id);
		if (user is null)
		{
			logger.LogWarning("Password reset failed: user {UserId} not found", model.Id);
			return new()
			{
				Succeeded = false,
				Errors = ["user not found."]
			};
		}
		var token = Encoding.UTF8.GetString(
			WebEncoders.Base64UrlDecode(model.Token));
		var result =
			await userManager.ResetPasswordAsync(user, token, model.NewPassword);
		if (!result.Succeeded)
		{
			logger.LogError(
							"Password reset failed for {UserId}. Errors: {Errors}",
							model.Id,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			return new()
			{
				Succeeded = false,
				Errors = [.. result.Errors.Select(e => e.Description)]
			};
		}
		logger.LogInformation("Password reset successful for {UserId}", user.Id);
		return new()
		{
			Succeeded = true
		};
	}
}
