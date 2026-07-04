namespace Midas.Api.Models;

public class Follow
{
    public string FollowerId { get; set; } = null!;
    public ApplicationUser Follower { get; set; } = null!;

    public string FollowingId { get; set; } = null!;
    public ApplicationUser Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
