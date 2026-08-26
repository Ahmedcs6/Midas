namespace Midas.Api.Helpers;

public class JwtSettings
{
	public const string SectionName = "JwtSettings";

	public string Issuer { get; init; } = null!;

	public string Audience { get; init; } = null!;

	public string Key { get; init; } = null!;

	public double AccessTokenLifetimeMinutes { get; init; }
}
