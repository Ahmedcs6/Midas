using System.IdentityModel.Tokens.Jwt;

namespace Midas.Api.Interfaces;

public interface IAuthService
{
	Task<AuthResult> RegisterAsync(RegisterDto model);
	Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user);
	Task<AuthResult> LoginAsync(LoginDto model);
	RefreshToken GenerateRefreshToken();
	Task<AuthResult> RefreshAsync(RefreshTokenRequest model);
	Task<AuthResult> ForgotPasswordAsync(ForgotPasswordDto model);
	Task SendConfirmEmailAsync(ConfirmEmailDto model);
	Task<AuthResult> ConfirmEmailAsync(string userId, string token);
	Task<AuthResult> ResetPasswordAsync(ResetPasswordDto model);
}
