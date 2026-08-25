using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Midas.Api.Services;

public sealed class EmailWorker(Channel<IEmailJob> channel, IServiceScopeFactory serviceScopeFactory, ILogger<EmailWorker> logger) : BackgroundService
{
	private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
	private readonly ILogger<EmailWorker> _logger = logger;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (var job in channel.Reader.ReadAllAsync())
		{
			try
			{
				await using var scope = _serviceScopeFactory.CreateAsyncScope();
				var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
				await emailSender.SendAsync(job, stoppingToken);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "failed to process email job");
			}
		}
	}
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation(">>> StopAsync called");

		var completed = channel.Writer.TryComplete();

		_logger.LogInformation($">>> Channel completed: {completed}");

		await base.StopAsync(cancellationToken);

		_logger.LogInformation(">>> Worker stopped");
	}
}
