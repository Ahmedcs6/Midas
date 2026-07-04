namespace Midas.Api.Models.Dtos;

public class ConfirmEmailDto
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
