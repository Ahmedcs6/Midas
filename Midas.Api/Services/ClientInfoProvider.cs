using UAParser;
namespace Midas.Api.Services;

public sealed class ClientInfoProvider(
	IHttpContextAccessor httpContextAccessor) : IClientInfoProvider
{
	public ClientInfo GetClientInfo()
	{
		var httpContext = httpContextAccessor.HttpContext;

		var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
		var uaParser = Parser.GetDefault();
		ClientInfo parsed = uaParser.Parse(userAgent);
		return parsed;
	}
}
