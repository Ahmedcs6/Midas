using System.Security.Claims;

namespace Midas.Api.Services;

public class CurrentUser(
	IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public Guid? UserId
	{
		get
		{
			var value = httpContextAccessor.HttpContext?
				.User
				.FindFirstValue(ClaimTypes.NameIdentifier);
			if (value is null)
				return null;
			return Guid.TryParse(value, out var id) ? id : null;
		}
	}
}
