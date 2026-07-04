namespace Midas.Api.Models.Dtos;

public class DeleteAccountDto
{
	[Required]
	public string Password { get; set; } = string.Empty;
}
