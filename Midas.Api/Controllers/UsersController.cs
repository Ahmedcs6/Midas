using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ICurrentUser currentUser, IUserService userService) : ControllerBase
{

	[HttpGet("{userName}")]
	public async Task<IActionResult> GetUser(string userName)
	{
		var result = await userService.GetByUserNameAsync(userName);
		return this.ToActionResult(result);
	}
	[HttpPut("me")]
	[Authorize]
	public async Task<IActionResult> Edit([FromBody] EditUserRequest request)
	{
		var result = await userService.EditAsync(currentUser.UserId, request);
		return this.ToActionResult(result);
	}
	[HttpPost("me/avatar")]
	[Authorize]
	public async Task<IActionResult> EditAvatar([FromForm] EditAvatarRequest request)
	{
		var result = await userService.EditAvatarAsync(currentUser.UserId, request);
		return this.ToActionResult(result);
	}
	[HttpPost("follow/{userName}")]
	[Authorize]
	public async Task<IActionResult> Follow(string userName)
	{
		var result = await userService.Follow(userName);
		return this.ToActionResult(result);
	}
	[HttpPost("unfollow/{userName}")]
	[Authorize]
	public async Task<IActionResult> Unfollow(string userName)
	{
		var result = await userService.Unfollow(userName);
		return this.ToActionResult(result);
	}
}
