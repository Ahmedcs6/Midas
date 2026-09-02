using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Midas.Api.Services;

public class CurrentUser(
	IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	public Guid UserId
	{
		get
		{
			var value = httpContextAccessor.HttpContext?
				.User
				.FindFirstValue(ClaimTypes.NameIdentifier);
			return Guid.TryParse(value, out var id) ? id : throw new UnsupportedContentTypeException("A7a");
		}
	}
}
