using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
using Midas.Api.Models.Dtos.Post.Request;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostController(IPostService postService) : ControllerBase
{
	[HttpPost("")]
	public async Task<IActionResult> CreatePost(CreatePostRequest request)
	{
		var response = await postService.CreatePost(request);
		return StatusCode(StatusCodes.Status201Created, ResponseHelper.Success(response));
	}
}
