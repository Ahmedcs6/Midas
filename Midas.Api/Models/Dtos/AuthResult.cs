
namespace Midas.Api.Models.Dtos;

public class AuthResult
{
	public List<string> Errors { get; set; } = [];

	public bool Succeeded { get; set; }

	public UserDto? User { get; set; }

	public RefreshTokenResponse? RefreshTokenResponse { get; set; }
}
