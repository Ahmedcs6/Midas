using Microsoft.AspNetCore.Mvc;
using Midas.Api.Helpers.Responses;
namespace Midas.Api.Extensions;

public static class ServiceResultExtensions
{
	public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result, int successStatusCode = StatusCodes.Status200OK)
	{
		return result.State switch
		{
			ServiceState.Success => controller.StatusCode(successStatusCode, new ApiResponse<T>
			{
				Success = true,
				Message = result.Message,
				Data = result.Data
			}),
			ServiceState.BadRequest => controller.BadRequest(new ApiResponse<T>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Unauthorized => controller.Unauthorized(new ApiResponse<T>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<T>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.NotFound => controller.NotFound(new ApiResponse<T>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Conflict => controller.Conflict(new ApiResponse<T>
			{
				Success = false,
				Message = result.Message
			}),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static IActionResult ToActionResult(this ControllerBase controller, ServiceResult result)
	{
		return result.State switch
		{
			ServiceState.Success => controller.Ok(new ApiResponse<object>
			{
				Success = true,
				Message = result.Message
			}),
			ServiceState.BadRequest => controller.BadRequest(new ApiResponse<object>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Unauthorized => controller.Unauthorized(new ApiResponse<object>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.NotFound => controller.NotFound(new ApiResponse<object>
			{
				Success = false,
				Message = result.Message
			}),
			ServiceState.Conflict => controller.Conflict(new ApiResponse<object>
			{
				Success = false,
				Message = result.Message
			}),
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}
