namespace Midas.Api.Models;

public class Follow
{
	public Guid FollowerId { get; set; }
	public ApplicationUser Follower { get; set; } = null!;

	public Guid FollowingId { get; set; }
	public ApplicationUser Following { get; set; } = null!;

	public DateTime CreatedAt { get; set; }
}
