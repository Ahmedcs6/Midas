namespace Midas.Api.Models.Dtos.Auth.Request;

public class ResetPasswordRequest
{
	public Guid Id { get; set; }
	public string Token { get; set; } = "";
	public string NewPassword { get; set; } = "";
}
