using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Threading.Channels;
using Microsoft.AspNetCore.WebUtilities;

namespace Midas.Api.Services;

public class AccountService(ILogger<AccountService> logger, Channel<IEmailJob> channel, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IJwtService jwtService) : IAccountService
{

	public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
	{
		await using var transaction = await context.Database.BeginTransactionAsync();
		ApplicationUser user = new()
		{
			FirstName = request.FirstName,
			LastName = request.LastName,
			UserName = request.UserName,
			Gender = request.Gender,
			Email = request.Email
		};
		var result = await userManager.CreateAsync(user, request.Password);

		if (!result.Succeeded)
		{
			logger.LogWarning(
							"Registration failed for {Email}. Errors: {Errors}",
							request.Email,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			var errors = result.Errors.ToList();

			var state = errors.Any(e =>
				e.Code is "DuplicateUserName" or "DuplicateEmail")
				? ServiceState.Conflict
				: ServiceState.BadRequest;

			return new()
			{
				State = state,
				Message = string.Join(", ", errors.Select(e => e.Description))
			};
		}
		result = await userManager.AddToRoleAsync(user, "User");
		if (!result.Succeeded)
		{
			logger.LogError(
						  "Failed to assign 'User' role to {UserId}. Errors: {Errors}",
						  user.Id,
						  string.Join(", ", result.Errors.Select(e => e.Description)));
			throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
		}
		await transaction.CommitAsync();
		logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);

		return new()
		{
			State = ServiceState.Success,
			Message = "Register Succeeded, please confirm your email.",
			Data = new()
			{
				FirstName = user.FirstName,
				LastName = user.LastName,
				UserName = user.UserName,
				Gender = user.Gender
			}
		};
	}
	public async Task<ServiceResult<RefreshTokenResponse>> LoginAsync(LoginRequest request)
	{
		var user = await userManager.FindByEmailAsync(request.Email);
		if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogWarning("Login blocked: email not confirmed for {Email}", request.Email);
			return new() { State = ServiceState.Forbidden, Message = "Please confirm your email." };
		}

		if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
		{
			logger.LogWarning("Failed login attempt for {Email}", request.Email);
			return new() { State = ServiceState.Unauthorized, Message = "Invalid email or password." };
		}
		logger.LogInformation("User logged in: {UserId} ({Email}) from client {Client}", user.Id, user.Email, request.Client);
		await context.RefreshTokens
						.Where(t => t.ApplicationUserId == user.Id && t.Client == request.Client &&
									t.RevokedAt == null)
						.ExecuteUpdateAsync(setters =>
							setters.SetProperty(
								t => t.RevokedAt,
								DateTime.UtcNow));
		logger.LogDebug("Revoked previous refresh tokens for {UserId} on client {Client}", user.Id, request.Client);
		var token = await jwtService.CreateJwtTokenAsync(user);
		var bytes = jwtService.GenerateRefreshToken();
		var refreshToken = new RefreshToken
		{
			TokenHash = Convert.ToBase64String(SHA256.HashData(bytes)),
			ExpiresAt = DateTime.UtcNow.AddDays(30),
			Client = request.Client,
			ApplicationUserId = user.Id
		};
		context.RefreshTokens.Add(refreshToken);
		await context.SaveChangesAsync();
		logger.LogInformation("Issued new refresh token for {UserId}, expires {ExpiresAt:O}", user.Id, refreshToken.ExpiresAt);
		return new()
		{
			State = ServiceState.Success,
			Data = new()
			{
				AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
				AccessTokenExpiresAt = token.ValidTo,
				RefreshToken = Convert.ToBase64String(bytes),
				RefreshTokenExpiresAt = refreshToken.ExpiresAt
			}
		};
	}
	public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
	{
		var user = await userManager.FindByEmailAsync(request.Email);
		if (user is null)
		{
			logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
			return new() { State = ServiceState.Success };
		}
		if (!await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogWarning("Password reset blocked: email not confirmed for {UserId}", user.Id);
			return new()
			{
				State = ServiceState.Forbidden,
				Message = "Please confirm your Email."
			};
		}
		var token = await userManager.GeneratePasswordResetTokenAsync(user);

		var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

		var resetLink = $"https://localhost:7103/reset-password?userId={user.Id}&token={encodedToken}";
		logger.LogInformation("Password reset link generated for {UserId}", user.Id);
		await channel.Writer.WriteAsync(new PasswordResetJob(user, request.Email, resetLink));
		return new()
		{
			State = ServiceState.Success
		};
	}
	public async Task<ServiceResult> SendConfirmEmailAsync(ConfirmEmailRequset request)
	{
		var user = await userManager.FindByEmailAsync(request.Email);
		if (user is null || await userManager.IsEmailConfirmedAsync(user))
		{
			logger.LogDebug("Confirmation email skipped for {Email}: user not found or already confirmed", request.Email);
			return new() { State = ServiceState.Success };
		}
		string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
		string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

		string confirmationLink = $"https://localhost:7103/confirm-email?userId={user.Id}&token={encodedToken}";
		logger.LogInformation("Sending confirmation email to {UserId} ({Email})", user.Id, user.Email);
		await channel.Writer.WriteAsync(new ConfirmEmailJob(user, request.Email, confirmationLink));
		return new() { State = ServiceState.Success };
	}
	public async Task<ServiceResult> ConfirmEmailAsync(Guid userId, string token)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
		{
			logger.LogWarning("Email confirmation failed: user {UserId} not found", userId);
			return new() { State = ServiceState.NotFound, Message = "user not found." };
		}
		token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

		var result = await userManager.ConfirmEmailAsync(user, token);

		if (!result.Succeeded)
		{
			logger.LogError(
							"Email confirmation failed for {UserId}. Errors: {Errors}",
							userId,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			return new()
			{
				State = ServiceState.BadRequest,
				Message = string.Join(", ", result.Errors.Select(e => e.Description))
			};
		}
		logger.LogInformation("Email confirmed for {UserId} ({Email})", user.Id, user.Email);
		return new()
		{
			State = ServiceState.Success
		};
	}
	public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
	{
		var user = await userManager.FindByIdAsync(request.Id);
		if (user is null)
		{
			logger.LogWarning("Password reset failed: user {UserId} not found", request.Id);
			return new()
			{
				State = ServiceState.NotFound,
				Message = "user not found."
			};
		}
		var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
		var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
		if (!result.Succeeded)
		{
			logger.LogError(
							"Password reset failed for {UserId}. Errors: {Errors}",
							request.Id,
							string.Join(", ", result.Errors.Select(e => e.Description)));
			return new()
			{
				State = ServiceState.BadRequest,
				Message = string.Join(", ", result.Errors.Select(e => e.Description))
			};
		}
		logger.LogInformation("Password reset successful for {UserId}", user.Id);
		return new()
		{
			State = ServiceState.Success
		};
	}
}
