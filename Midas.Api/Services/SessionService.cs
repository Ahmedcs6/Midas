namespace Midas.Api.Services;

public class SessionService(
	ILogger<SessionService> logger,
	ApplicationDbContext context,
	ICurrentUser currentUser
) : ISessionService
{
	public async Task<Result<List<SessionResponse>>> ListAsync()
	{
		if (currentUser.UserId is null)
			return new()
			{
				Success = false,
				Error = Error.AuthenticationRequired,
				Message = "Authentication required.",
			};

		var sessions = await context
			.Sessions.AsNoTracking()
			.Where(s => s.UserId == currentUser.UserId && s.RevokedAt == null)
			.OrderByDescending(s => s.CreatedAt)
			.Select(s => new SessionResponse
			{
				Id = s.Id,
				Client = s.Client.ToString(),
				OperatingSystem = s.OperatingSystem,
				Browser = s.Browser,
				Device = s.Device,
				IpAddress = s.IpAddress,
				CreatedAt = s.CreatedAt,
				LastActivityAt = s.LastActivityAt,
				IsCurrent = s.Id == currentUser.SessionId,
			})
			.ToListAsync();

		return new() { Success = true, Data = sessions };
	}

	public async Task<Result> RevokeOneAsync(Guid sessionId)
	{
		if (currentUser.UserId is null)
			return new()
			{
				Success = false,
				Error = Error.AuthenticationRequired,
				Message = "Authentication required.",
			};

		var now = DateTime.UtcNow;

		var rowsAffected = await context
			.Sessions.Where(s =>
				s.Id == sessionId && s.UserId == currentUser.UserId && s.RevokedAt == null
			)
			.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedAt, now));

		if (rowsAffected == 0)
			return new()
			{
				Success = false,
				Error = Error.NotFound,
				Message = "Session not found.",
			};

		await context
			.RefreshTokens.Where(t => t.SessionId == sessionId && t.RevokedAt == null)
			.ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));

		logger.LogInformation(
			"Session revoked: {SessionId} for {UserId}",
			sessionId,
			currentUser.UserId
		);
		return new() { Success = true };
	}

	public async Task<Result> RevokeAllExceptCurrentAsync()
	{
		if (currentUser.UserId is null)
			return new()
			{
				Success = false,
				Error = Error.AuthenticationRequired,
				Message = "Authentication required.",
			};

		var now = DateTime.UtcNow;
		var currentSessionId = currentUser.SessionId;

		await context
			.RefreshTokens.Where(t =>
				t.Session.UserId == currentUser.UserId
				&& t.SessionId != currentSessionId
				&& t.Session.RevokedAt == null
				&& t.RevokedAt == null
			)
			.ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));

		await context
			.Sessions.Where(s =>
				s.UserId == currentUser.UserId && s.Id != currentSessionId && s.RevokedAt == null
			)
			.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedAt, now));

		logger.LogInformation(
			"All sessions except current revoked for {UserId}",
			currentUser.UserId
		);
		return new() { Success = true };
	}

	public async Task<Result> RevokeAllAsync(Guid userId)
	{
		var now = DateTime.UtcNow;

		await context
			.RefreshTokens.Where(t =>
				t.Session.UserId == userId && t.Session.RevokedAt == null && t.RevokedAt == null
			)
			.ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now));

		await context
			.Sessions.Where(s => s.UserId == userId && s.RevokedAt == null)
			.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedAt, now));

		logger.LogInformation("All sessions revoked for {UserId}", userId);
		return new() { Success = true };
	}
}
