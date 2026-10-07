using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Midas.Api.Middlewares;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
	Log.Information("Starting the application...");
	var builder = WebApplication.CreateBuilder(args);

	builder.Host.UseSerilog(
		(context, configuration) =>
		{
			configuration.ReadFrom.Configuration(context.Configuration);
		}
	);

	builder
		.Services.AddAppOptions(builder.Configuration)
		.AddPersistence(builder.Configuration)
		.AddAppIdentityAuth()
		.AddEmailQueue()
		.AddApplicationServices()
		.AddWebApi();

	var app = builder.Build();

	app.UseSerilogRequestLogging(options =>
	{
		options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
		{
			var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
			diagnosticContext.Set("UserId", userId ?? "anonymous");
			var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
			if (!string.IsNullOrEmpty(remoteIp))
				diagnosticContext.Set("ClientIP", remoteIp);
		};
	});

	app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
	if (app.Environment.IsDevelopment())
	{
		app.MapOpenApi();
		app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Swagger"));
	}

	// app.UseHttpsRedirection();
	// app.UseStaticFiles();
	app.MapStaticAssets();
	app.UseAuthentication();
	app.UseAuthorization();

	app.MapControllers();

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
	Environment.ExitCode = 1;
}
finally
{
	Log.CloseAndFlush();
}
