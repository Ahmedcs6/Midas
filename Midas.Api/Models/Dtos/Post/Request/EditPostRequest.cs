namespace Midas.Api.Models.Dtos.Post.Request;

public class EditPostRequest
{
	public string? Content { get; set; }
	public IFormFile? Image { get; set; }
	public bool RemoveImage { get; set; }
}
