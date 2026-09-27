namespace Midas.Api.Models.Dtos.Auth.Request;

public class RegisterRequest
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public Gender Gender { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}
