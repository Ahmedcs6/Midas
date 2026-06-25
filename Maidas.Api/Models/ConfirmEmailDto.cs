namespace Maidas.Api.Models;

public class ConfirmEmailDto
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
