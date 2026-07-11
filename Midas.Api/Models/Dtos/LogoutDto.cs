namespace Midas.Api.Models.Dtos;

public class LogoutDto
{
	[Required]
	public string RefreshToken { get; set; } = "";
}
