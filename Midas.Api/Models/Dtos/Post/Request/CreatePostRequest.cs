namespace Midas.Api.Models.Dtos.Post.Request;

public class CreatePostRequest
{
	public string Content { get; set; } = "";
	public Privacy Privacy { get; set; }
	public IFormFile? Image { get; set; }
}
