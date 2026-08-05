namespace Midas.Api.Models.Dtos.Auth.Response;

public class RefreshTokenResponse
{
	public string AccessToken { get; set; } = "";

	public DateTime AccessTokenExpiresAt { get; set; }

	public string RefreshToken { get; set; } = "";

	public DateTime RefreshTokenExpiresAt { get; set; }
}
