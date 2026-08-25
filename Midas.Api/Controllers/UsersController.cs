using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ICurrentUser currentUser, IUserService userService) : ControllerBase
{

	[HttpGet("{userName}")]
	public async Task<IActionResult> GetUser(string userName)
	{
		UserResponse? response = await userService.GetByUserNameAsync(userName);
		return response is null ?
			BadRequest(ResponseHelper.Fail("User not found.")) :
			Ok(ResponseHelper.Success(response));
	}
	[HttpPut("me")]
	[Authorize]
	public async Task<IActionResult> Edit([FromBody] EditUserRequest request)
	{
		var result = await userService.EditAsync(currentUser.UserId!, request);
		return result ? Ok(ResponseHelper.Success<object>(new { })) : Unauthorized();
	}
	[HttpPost("me/avatar")]
	[Authorize]
	public async Task<IActionResult> EditAvatar([FromForm] EditAvatarRequest request)
	{
		var result = await userService.EditAvatarAsync(currentUser.UserId!, request);
		return result ? Ok(ResponseHelper.Success<object>(new { })) : BadRequest();
	}
	[HttpPost("follow/{userName}")]
	[Authorize]
	public async Task<IActionResult> Follow(string userName)
	{
		var result = await userService.Follow(userName);
		return result ? Ok(ResponseHelper.Success(new { })) : BadRequest();
	}
}
