namespace Midas.Api.Models.Dtos.User.Response;

public class SessionResponse
{
	public Guid Id { get; set; }

	public string Client { get; set; } = string.Empty;

	public string? OperatingSystem { get; set; }

	public string? Browser { get; set; }

	public string? Device { get; set; }

	public string? IpAddress { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime LastActivityAt { get; set; }

	public bool IsCurrent { get; set; }
}
