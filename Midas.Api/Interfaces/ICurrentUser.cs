namespace Midas.Api.Interfaces;

public interface ICurrentUser
{
	Guid? UserId { get; }
	Guid? SessionId { get; }
}
