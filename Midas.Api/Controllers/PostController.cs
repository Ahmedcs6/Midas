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
		var result = await postService.CreatePostAsync(request);
		return this.ToActionResult(result, StatusCodes.Status201Created);
	}
	[HttpPatch("{id}")]
	public async Task<IActionResult> EditPost(int id, EditPostRequest request)
	{
		var result = await postService.EditPostAsync(id, request);
		return this.ToActionResult(result);
	}

}
