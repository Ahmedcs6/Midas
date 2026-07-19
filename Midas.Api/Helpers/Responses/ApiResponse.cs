namespace Midas.Api.Helpers.Responses;

public class ApiResponse<T>(
	bool success,
	string message,
	T? data = default,
	List<string>? errors = null)
{
	public bool Success { get; init; } = success;

	public string Message { get; init; } = message;

	public T? Data { get; init; } = data;

	public List<string>? Errors { get; init; } = errors;
}
