namespace Midas.Api.Models.Dtos.Auth.Request;

public class ForgotPasswordRequest
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
