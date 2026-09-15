namespace Midas.Api.Models.Dtos.Auth.Request;

public class RefreshTokenRequest
{
	[Required, MinLength(1)]
	public string RefreshToken { get; set; } = string.Empty;
}
