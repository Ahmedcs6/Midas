using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;

	[HttpGet("{userName}")]
	public async Task<IActionResult> GetUser(string userName)
	{
		ApplicationUser? user = null;
		if (userName == "me")
		{
			var me = await _userManager.GetUserAsync(User);
			if (me is null)
			{
				return BadRequest("You are logged out.");
			}
			else
				user = me;
		}
		user ??= await _userManager.FindByNameAsync(userName);
		if (user is null)
			return BadRequest("User not found.");
		UserDto response = new()
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
