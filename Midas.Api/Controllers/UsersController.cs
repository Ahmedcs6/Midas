using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ICurrentUser currentUser, IUserService userService, IPostService postService, ISessionService sessionService) : ControllerBase
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
		var result = await userService.EditAsync((Guid)currentUser.UserId!, request);
		return this.ToActionResult(result);
	}
	[HttpPost("me/avatar")]
	public async Task<IActionResult> EditAvatar([FromForm] EditAvatarRequest request)
	{
		var result = await userService.EditAvatarAsync((Guid)currentUser.UserId!, request);
		return this.ToActionResult(result);
	}
	[HttpPost("follow/{userName}")]
	public async Task<IActionResult> Follow(string userName)
	{
		var result = await userService.Follow(userName);
		return this.ToActionResult(result);
	}
	[HttpDelete("follow/{userName}")]
	public async Task<IActionResult> Unfollow(string userName)
	{
		var result = await userService.Unfollow(userName);
		return this.ToActionResult(result, StatusCodes.Status204NoContent);
	}
	[HttpGet("{userName}/posts")]
	[AllowAnonymous]
	public async Task<IActionResult> Posts(string userName, [FromQuery][Range(1, 50)] int limit = 10, [FromQuery][Range(1, int.MaxValue)] int? cursor = null)
	{
		var result = await postService.GetPostsAsync(userName, limit, cursor);
		return this.ToActionResult(result);
	}
	[HttpGet("me/sessions")]
	public async Task<IActionResult> Sessions()
	{
		var result = await sessionService.ListAsync();
		return this.ToActionResult(result);
	}
	[HttpDelete("me/sessions/{id:guid}")]
	public async Task<IActionResult> RevokeSession(Guid id)
	{
		var result = await sessionService.RevokeOneAsync(id);
		return this.ToActionResult(result, StatusCodes.Status204NoContent);
	}
	[HttpDelete("me/sessions")]
	public async Task<IActionResult> RevokeSessions()
	{
		var result = await sessionService.RevokeAllExceptCurrentAsync();
		return this.ToActionResult(result, StatusCodes.Status204NoContent);
	}
}
