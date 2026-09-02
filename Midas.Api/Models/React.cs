namespace Midas.Api.Models;

public class React
{
	public int PostId { get; set; }

	public Guid UserId { get; set; }

	public Post Post { get; set; } = null!;

	public ApplicationUser User { get; set; } = null!;
}
