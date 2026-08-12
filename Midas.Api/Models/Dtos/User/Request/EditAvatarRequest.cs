namespace Midas.Api.Models.Dtos.User.Request;

public class EditAvatarRequest
{
	public IFormFile Image { get; set; } = default!;
}
