using System.Security.Claims;

namespace Midas.Api.Services;

public class CurrentUser(
	IHttpContextAccessor httpContextAccessor) : ICurrentUser
{

	public string? UserId =>
		httpContextAccessor.HttpContext?
			.User
			.FindFirstValue(ClaimTypes.NameIdentifier);
}
