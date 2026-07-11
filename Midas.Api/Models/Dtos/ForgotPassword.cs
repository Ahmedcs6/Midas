namespace Midas.Api.Models.Dtos;

public class ForgotPasswordDto
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
