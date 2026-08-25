using System.Threading;

namespace Midas.Api.Interfaces;

public interface IEmailSender
{
	Task SendAsync(IEmailJob job, CancellationToken cancellationToken);
}
