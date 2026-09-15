namespace Midas.Api.Models;

public class Session
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }
	public ApplicationUser User { get; set; } = null!;

	public Client Client { get; set; }

	public string? OperatingSystem { get; set; }
	public string? Browser { get; set; }

	public string? Device { get; set; }

	public string? IpAddress { get; set; }

	public DateTime CreatedAt { get; set; }
	public DateTime LastActivityAt { get; set; }

	public DateTime? RevokedAt { get; set; }

	public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
