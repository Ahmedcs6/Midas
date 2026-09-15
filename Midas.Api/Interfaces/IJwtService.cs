using System.IdentityModel.Tokens.Jwt;

namespace Midas.Api.Interfaces;

public interface IJwtService
{
	Task<JwtSecurityToken> CreateJwtTokenAsync(ApplicationUser user, Guid sessionId);
	byte[] GenerateRefreshToken();
	Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest model);
}
