namespace Midas.Api.Interfaces;

public interface IAccountService
{
	Task<Result<UserResponse>> RegisterAsync(RegisterRequest request);
	Task<Result<RefreshTokenResponse>> LoginAsync(LoginRequest request);
	Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
	Task<Result> SendConfirmEmailAsync(ConfirmEmailRequest request);
	Task<Result> ConfirmEmailAsync(Guid userId, string token);
	Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
}
