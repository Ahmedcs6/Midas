using System.Text.Json.Serialization;

namespace Midas.Api.Models.Dtos;

public class AuthResult
{
	public IEnumerable<string> Errors { get; set; } = [];

	public bool Succeeded { get; set; }

	[JsonIgnore]
	public ApplicationUser User { get; set; } = new();

	public string AccessToken { get; set; } = "";

	public DateTime ExpiresOn { get; set; }

	public RefreshToken? RefreshToken { get; set; } = new();
}
