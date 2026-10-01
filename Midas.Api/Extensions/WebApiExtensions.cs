using System.Text.Json.Serialization;
using FluentValidation;
using Midas.Api.Validators;

namespace Midas.Api.Extensions;

public static class WebApiExtensions
{
	public static IServiceCollection AddWebApi(this IServiceCollection services)
	{
		services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

		services
			.AddControllers(options =>
			{
				options.Filters.Add<FluentValidationFilter>();
			})
			.AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
		services.AddOpenApi();

		return services;
	}
}
