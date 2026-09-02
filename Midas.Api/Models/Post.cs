namespace Midas.Api.Models;

public class Post
{
	public int Id { get; set; }

	public string Content { get; set; } = null!;

	public DateTime PublishDate { get; set; }

	public string? ImageUrl { get; set; }

	public PrivacyType Privacy { get; set; }

	public ICollection<Comment> Comments { get; set; } = [];

	public ICollection<React> Reacts { get; set; } = [];

	public Guid UserId { get; set; }

	public ApplicationUser User { get; set; } = null!;
}
