
namespace Midas.Api.Models;

public class RefreshToken
{
	public int Id { get; set; }

	public ClientType Client { get; set; }

	public string TokenHash { get; set; } = "";

	public DateTime ExpiresAt { get; set; }

	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

	public DateTime? RevokedAt { get; set; }

	public bool IsActive => RevokedAt == null && !IsExpired;

	public string? ApplicationUserId { get; set; }

	public ApplicationUser? User { get; set; }
}
