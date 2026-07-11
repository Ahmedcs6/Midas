namespace Midas.Api.Models.Dtos;

public class ResetPasswordDto
{
	[Required]
	public string Id { get; set; } = "";
	[Required]
	public string Token { get; set; } = "";
	[Required]
	public string NewPassword { get; set; } = "";
}
