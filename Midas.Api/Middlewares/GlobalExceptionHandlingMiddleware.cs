using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Midas.Api.Middlewares;

public class GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger) : IMiddleware
{
	private readonly ILogger _logger = logger;

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (Exception e)
		{
			_logger.LogError(e, e.Message);
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			ProblemDetails problem = new()
			{
				Status = (int)HttpStatusCode.InternalServerError,
				Type = "Server error",
				Title = "Server error",
				Detail = "An internal Server Error has occured"

			};
			string json = JsonSerializer.Serialize(problem);
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(json);
		}
	}
}

