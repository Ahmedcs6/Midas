using Microsoft.Extensions.Configuration;

namespace Midas.Api.Extensions;

public static class OptionsExtensions
{
	public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

		services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

		services
			.AddOptions<JwtSettings>()
			.Bind(configuration.GetSection(JwtSettings.SectionName))
			.ValidateDataAnnotations()
			.Validate(s => !string.IsNullOrEmpty(s.Key), "JwtSettings:Key is required")
			.ValidateOnStart();

		return services;
	}
}
