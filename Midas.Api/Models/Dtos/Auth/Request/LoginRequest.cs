namespace Midas.Api.Models.Dtos.Auth.Request;

public class LoginRequest
{
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public Client? Client { get; set; }
}
