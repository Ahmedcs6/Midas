namespace Midas.Api.Models.Dtos.Post.Request;

public class CreatePostRequest
{
	public string Content { get; set; } = "";
	public PrivacyType Privacy { get; set; }
	public IFormFile? Image { get; set; }
}
