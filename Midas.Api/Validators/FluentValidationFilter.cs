using FluentValidation;
using Midas.Api.Helpers.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Midas.Api.Validators;

/// <summary>
/// Runs the matching FluentValidation validator for every action argument.
/// Returns 400 in the app's ApiResponse shape on failure.
/// (The FluentValidation.AspNetCore auto-validation package has no
/// net10-compatible release, so this filter does its job.)
/// </summary>
public class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		foreach (var argument in context.ActionArguments.Values)
		{
			if (argument is null)
				continue;

			var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
			if (serviceProvider.GetService(validatorType) is not IValidator validator)
				continue;

			var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
			if (result.IsValid)
				continue;

			context.Result = new BadRequestObjectResult(new ApiResponse<object>
			{
				Success = false,
				Message = string.Join(", ", result.Errors.Select(e => e.ErrorMessage))
			});
			return;
		}

		await next();
	}
}
