namespace Midas.Api.Models.Dtos.Auth.Request;

public class ForgotPasswordRequest
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;
}
