using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.User.Request;

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
		return Ok(ResponseHelper.Success(response));
	}
	[HttpPut("me")]
	[Authorize]
	public async Task<IActionResult> Edit([FromBody] EditUserRequest model)
	{
		var me = await _userManager.GetUserAsync(User);
		if (me is null) return Unauthorized();
		me.FirstName = model.FirstName;
		me.LastName = model.LastName;
		me.About = model.About;
		me.Address = model.Address;
		me.BirthDate = model.BirthDate;
		await _userManager.UpdateAsync(me);
		return Ok(ResponseHelper.Success<object>(new { }));
	}
}
