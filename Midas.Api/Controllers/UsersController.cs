using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
namespace Midas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(ICurrentUser currentUser, IUserService userService) : ControllerBase
{
	private readonly ICurrentUser _currentUser = currentUser;
	private readonly IUserService _userService = userService;

	[HttpGet("{userName}")]
	public async Task<IActionResult> GetUser(string userName)
	{
		UserResponse? response = await _userService.GetByUserNameAsync(userName);
		return response is null ?
			BadRequest(ResponseHelper.Fail("User not found.")) :
			Ok(ResponseHelper.Success(response));
	}
	[HttpPut("me")]
	[Authorize]
	public async Task<IActionResult> Edit([FromBody] EditUserRequest request)
	{
		var result = await _userService.EditAsync(_currentUser.UserId!, request);
		return result ? Ok(ResponseHelper.Success<object>(new { })) : Unauthorized();
	}
	[HttpPost("me/Avatar")]
	[Authorize]
	public async Task<IActionResult> EditAvatar([FromForm] EditAvatarRequest request)
	{
		var result = await _userService.EditAvatarAsync(_currentUser.UserId!, request);
		return result ? Ok(ResponseHelper.Success<object>(new { })) : BadRequest();
	}
}
