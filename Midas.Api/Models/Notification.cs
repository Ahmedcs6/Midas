namespace Midas.Api.Models;

public class Notification
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }
	public ApplicationUser User { get; set; } = null!;

	public string Content { get; set; } = null!;

	public bool IsRead { get; set; } = false;

	public DateTime CreatedAt { get; set; }
}
