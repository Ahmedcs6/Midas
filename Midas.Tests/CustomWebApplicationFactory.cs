using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
	}
}
