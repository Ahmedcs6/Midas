using System.IdentityModel.Tokens.Jwt;

namespace Midas.Api.Interfaces;

public interface IAuthService
{
	Task<AuthResult> RegisterAsync(RegisterDto model);
	Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user);
	Task<AuthResult> LoginAsync(LoginDto model);
	RefreshToken GenerateRefreshToken();
	Task<AuthResult> Refresh(RefreshTokenRequest model);
}
