using UAParser;

namespace Midas.Api.Interfaces;

public interface IClientInfoProvider
{
	ClientInfo GetClientInfo();

	string? GetIpAddress();
}
