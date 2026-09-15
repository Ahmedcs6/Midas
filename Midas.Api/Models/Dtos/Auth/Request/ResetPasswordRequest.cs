namespace Midas.Api.Models.Dtos.Auth.Request;

public class ResetPasswordRequest
{
	[Required]
	public Guid Id { get; set; }
	[Required]
	public string Token { get; set; } = "";
	[Required]
	public string NewPassword { get; set; } = "";
}
