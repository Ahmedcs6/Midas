namespace Midas.Api.Models.Dtos;

public class ServiceResult
{
	public ServiceState State { get; init; }
	public string? Message { get; init; }
}

public class ServiceResult<T> : ServiceResult
{
	public T? Data { get; init; }
}
