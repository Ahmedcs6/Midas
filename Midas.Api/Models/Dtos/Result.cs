namespace Midas.Api.Models.Dtos;

public class Result
{
	public bool Success { get; set; }
	public ErrorType Error { get; init; }
	public string? Message { get; init; }
}

public class Result<T> : Result
{
	public T? Data { get; init; }
}
