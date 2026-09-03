
namespace Midas.Api.Helpers.Responses;

public class ApiResponse<T>
{
	public bool Success { get; init; }

	public string? Message { get; init; } = "";

	public T? Data { get; init; } = default;
}
