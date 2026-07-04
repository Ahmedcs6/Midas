namespace Midas.Api.Models;

public class React
{
    public int PostId { get; set; }

    public string UserId { get; set; } = null!;

    public Post Post { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}
