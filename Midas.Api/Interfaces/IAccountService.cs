namespace Midas.Api.Interfaces;

public interface IAccountService
{
	Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request);
	Task<ServiceResult<RefreshTokenResponse>> LoginAsync(LoginRequest request);
	Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request);
	Task<ServiceResult> SendConfirmEmailAsync(ConfirmEmailRequset request);
	Task<ServiceResult> ConfirmEmailAsync(Guid userId, string token);
	Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
}
