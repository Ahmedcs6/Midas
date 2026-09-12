using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
namespace Midas.Api.Extensions;

public static class ResultExtensions
{

	public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, int successStatusCode = StatusCodes.Status200OK)
	{
		return result.Success
			? controller.StatusCode(
				successStatusCode,
				new ApiResponse<T>
				{
					Success = true,
					Message = result.Message,
					Data = result.Data
				})
			: result.Error switch
			{
				ErrorType.Validation => controller.BadRequest(
					new ApiResponse<T>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.AuthenticationRequired => controller.Unauthorized(
					new ApiResponse<T>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.AccessDenied => controller.StatusCode(
					StatusCodes.Status403Forbidden,
					new ApiResponse<T>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.NotFound => controller.NotFound(
					new ApiResponse<T>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.Conflict => controller.Conflict(
					new ApiResponse<T>
					{
						Success = false,
						Message = result.Message
					}),

				_ => throw new ArgumentOutOfRangeException()
			};
	}

	public static IActionResult ToActionResult(this ControllerBase controller, Result result, int successStatusCode = StatusCodes.Status200OK)
	{
		return result.Success
			? controller.StatusCode(
				successStatusCode,
				new ApiResponse<object>
				{
					Success = true,
					Message = result.Message
				})
			: result.Error switch
			{
				ErrorType.Validation => controller.BadRequest(
					new ApiResponse<object>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.AuthenticationRequired => controller.Unauthorized(
					new ApiResponse<object>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.AccessDenied => controller.StatusCode(
					StatusCodes.Status403Forbidden,
					new ApiResponse<object>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.NotFound => controller.NotFound(
					new ApiResponse<object>
					{
						Success = false,
						Message = result.Message
					}),

				ErrorType.Conflict => controller.Conflict(
					new ApiResponse<object>
					{
						Success = false,
						Message = result.Message
					}),

				_ => throw new ArgumentOutOfRangeException()
			};
	}
}
