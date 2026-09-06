using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ICurrentUser currentUser, IUserService userService, IPostService postService) : ControllerBase
{
	[HttpGet("{userName}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetUser(string userName)
	{
		var result = await userService.GetByUserNameAsync(userName);
		return this.ToActionResult(result);
	}
	[HttpPatch("me")]
	public async Task<IActionResult> Edit([FromBody] EditUserRequest request)
	{
		var result = await userService.EditAsync(currentUser.UserId, request);
		return this.ToActionResult(result);
	}
	[HttpPost("me/avatar")]
	public async Task<IActionResult> EditAvatar([FromForm] EditAvatarRequest request)
	{
		var result = await userService.EditAvatarAsync(currentUser.UserId, request);
		return this.ToActionResult(result);
	}
	[HttpPost("follow/{userName}")]
	public async Task<IActionResult> Follow(string userName)
	{
		var result = await userService.Follow(userName);
		return this.ToActionResult(result);
	}
	[HttpPost("unfollow/{userName}")]
	public async Task<IActionResult> Unfollow(string userName)
	{
		var result = await userService.Unfollow(userName);
		return this.ToActionResult(result);
	}
	[HttpGet("{userName}/posts")]
	[AllowAnonymous]
	public async Task<IActionResult> Posts(string userName, int limit = 10, int? cursor = null)
	{
		var result = await postService.GetPostsAsync(userName, limit, cursor);
		return this.ToActionResult(result);
	}
}
