
namespace Midas.Api.Models;

public class RefreshToken
{
	public Guid Id { get; set; }

	public string TokenHash { get; set; } = null!;

	public DateTime ExpiresAt { get; set; }

	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

	public DateTime? RevokedAt { get; set; }

	public bool IsActive => RevokedAt == null && !IsExpired;

	public Guid SessionId { get; set; }

	public Session Session { get; set; } = null!;
}
