namespace Maidas.Api.Models;

public class Comment
{
    public int Id { get; set; }

    public string Content { get; set; } = null!;

    public int PostId { get; set; }

    public string? UserId { get; set; }

    public Post Post { get; set; } = null!;

    public ApplicationUser? User { get; set; }

}
