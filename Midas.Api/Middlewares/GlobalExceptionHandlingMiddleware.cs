using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Middlewares;

public class GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger) : IMiddleware
{
	private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception e)
		{
			_logger.LogError(
				e,
				"Unhandled exception while processing {Method} {Path}",
				context.Request.Method,
				context.Request.Path);
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			ProblemDetails problem = new()
			{
				Status = 500,
				Title = "Internal Server Error",
				Type = "https://httpstatuses.com/500",
				Detail = "An unexpected error occurred.",
				Instance = context.Request.Path
			};
			string json = JsonSerializer.Serialize(problem);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(json);
		}
	}
}

