using System.IdentityModel.Tokens.Jwt;

namespace Midas.Api.Interfaces;

public interface IJwtService
{
	Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user);
	byte[] GenerateRefreshToken();
	Task<AuthResult> RefreshAsync(RefreshTokenRequest model);
}
