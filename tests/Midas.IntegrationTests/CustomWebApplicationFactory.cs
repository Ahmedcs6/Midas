using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
		builder.ConfigureServices(services =>
		{
			// Replace (not duplicate) the real EmailSender so tests never hit SMTP.
			// Singleton Fake doubles as a spy: tests can assert channel jobs were sent.
			// EmailWorker (BackgroundService) is intentionally kept running.
			services.RemoveAll<IEmailSender>();
			services.AddSingleton<FakeEmailSender>();
			services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());
		});
	}
}
