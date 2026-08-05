namespace Midas.Api.Models.Dtos.Auth.Request;

public class ConfirmEmailRequset
{
	[Required]
	public string Email { get; set; } = string.Empty;
}
