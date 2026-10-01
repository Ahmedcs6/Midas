using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Midas.Api.Services;

public sealed class EmailWorker(Channel<IEmailJob> channel, IServiceScopeFactory serviceScopeFactory, ILogger<EmailWorker> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
		{
			try
			{
				await using var scope = serviceScopeFactory.CreateAsyncScope();
				var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
				await emailSender.SendAsync(job, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to process email job {JobType}", job.GetType().Name);
			}
		}
	}
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		channel.Writer.TryComplete();
		await base.StopAsync(cancellationToken);

	}
}
