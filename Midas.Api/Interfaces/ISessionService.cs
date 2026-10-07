namespace Midas.Api.Interfaces;

public interface ISessionService
{
	Task<Result<List<SessionResponse>>> ListAsync();
	Task<Result> RevokeOneAsync(Guid sessionId);
	Task<Result> RevokeAllExceptCurrentAsync();
	Task<Result> RevokeAllAsync(Guid userId);
}
