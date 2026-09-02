namespace Midas.Api.Models.Dtos.Post.Response;

public class PostResponse
{
	public int Id { get; set; }
	public DateTime PublishDate { get; set; }
	public string Content { get; set; } = "";
	public string ImageUrl { get; set; } = "";
	public string Privacy { get; set; } = "";
}
