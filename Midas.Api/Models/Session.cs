public class Session
{
	public Guid Id { get; set; }

	public Guid UserId { get; set; }

	public string? UserAgent { get; set; }

	public string? IpAddress { get; set; }

	public Client Client { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime LastUsedAt { get; set; }

	public DateTime ExpiresAt { get; set; }

	public DateTime? RevokedAt { get; set; }
}
