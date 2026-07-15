namespace Midas.Api.Interfaces;

public interface IAccountService
{

	Task<AuthResult> RegisterAsync(RegisterDto model);
	Task<AuthResult> LoginAsync(LoginDto model);
	Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto model);
	Task SendConfirmEmailAsync(ConfirmEmailDto model);
	Task<AuthResult> ConfirmEmailAsync(string userId, string token);
	Task<AuthResult> ResetPasswordAsync(ResetPasswordDto model);
}
