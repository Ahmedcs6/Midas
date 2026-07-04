using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Midas.Api.Models;

[Owned]
public class RefreshToken
{
	[JsonIgnore]
	public int Id { get; set; }
	public ClientType Client { get; set; }

	[NotMapped]
	public string Token { get; set; } = "";

	[JsonIgnore]
	public string TokenHash { get; set; } = "";

	public DateTime ExpiresAt { get; set; }

	[JsonIgnore]
	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

	[JsonIgnore]
	public DateTime? RevokedAt { get; set; }

	[JsonIgnore]
	public bool IsActive => RevokedAt == null && !IsExpired;

	[JsonIgnore]
	public string? ApplicationUserId { get; set; }

	[JsonIgnore]
	public ApplicationUser? User { get; set; }
}
