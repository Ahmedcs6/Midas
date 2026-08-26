using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Midas.Api.Services;

public sealed class EmailWorker(Channel<IEmailJob> channel, IServiceScopeFactory serviceScopeFactory, ILogger<EmailWorker> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var job in channel.Reader.ReadAllAsync(CancellationToken.None))
		{
			try
			{
				await using var scope = serviceScopeFactory.CreateAsyncScope();
				var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
				await emailSender.SendAsync(job, CancellationToken.None);
			}
			catch (Exception e)
			{
				logger.LogError(e, "failed to process email job");
			}
		}
	}
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var completed = channel.Writer.TryComplete();
		await base.StopAsync(cancellationToken);

	}
}
