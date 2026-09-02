using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Midas.Api.Interfaces;

namespace Midas.Tests;

public class CustomWebApplicationFactory
	: WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureAppConfiguration((context, config) =>
		{
			config.AddUserSecrets<Program>(
				optional: false);
		});
		builder.ConfigureServices(options =>
		{
			options.AddScoped<IEmailSender, FakeEmailSender>();
		});
	}
}
