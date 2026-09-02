namespace Midas.Api.Models;

public class Comment
{
	public int Id { get; set; }

	public DateTime CreatedAt { get; set; }

	public string Content { get; set; } = null!;

	public int PostId { get; set; }

	public Guid? UserId { get; set; }

	public Post Post { get; set; } = null!;

	public ApplicationUser? User { get; set; }

}
