namespace Midas.Api.Interfaces;

public interface IAccountService
{
	Task<AuthResult> RegisterAsync(RegisterRequest model);
	Task<AuthResult> LoginAsync(LoginRequest model);
	Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest model);
	Task SendConfirmEmailAsync(ConfirmEmailRequset model);
	Task<AuthResult> ConfirmEmailAsync(Guid userId, string token);
	Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest model);
}
