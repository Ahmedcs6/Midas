namespace Midas.Api.Helpers.Responses;

public static class ResponseHelper
{
	public static ApiResponse<T> Success<T>(
		T data,
		string message = "Request Success.")
	{
		return new(true, message, data);
	}

	public static ApiResponse<T> Fail<T>(
		string message,
		List<string>? errors = null)
	{
		return new(false, message, default, errors);
	}
}
