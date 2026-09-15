namespace Midas.Api.Models.Dtos.Auth.Request;

public class ConfirmEmailRequest
{
	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;
}
