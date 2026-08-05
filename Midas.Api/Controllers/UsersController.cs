using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;

namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;

	[HttpGet("{userName}")]
	public async Task<IActionResult> GetUser(string userName)
	{
		ApplicationUser? user = await _userManager.FindByNameAsync(userName);
		if (user is null)
			return BadRequest(ResponseHelper.Fail("User not found."));
		UserResponse response = new()
		{
			FirstName = user.FirstName,
			LastName = user.LastName,
			UserName = user.UserName!,
			Gender = user.Gender,
			About = user.About,
			Address = user.Address,
			BirthDate = user.BirthDate,
			ImageUrl = user.ImageUrl
		};
		return Ok(response);
	}
}
