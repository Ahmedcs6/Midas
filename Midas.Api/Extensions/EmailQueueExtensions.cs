using System.Threading.Channels;

namespace Midas.Api.Extensions;

public static class EmailQueueExtensions
{
	public static IServiceCollection AddEmailQueue(this IServiceCollection services)
	{
		services.AddSingleton(
			Channel.CreateBounded<IEmailJob>(
				new BoundedChannelOptions(100)
				{
					FullMode = BoundedChannelFullMode.Wait,
					SingleReader = true,
					SingleWriter = false,
				}
			)
		);
		services.AddHostedService<EmailWorker>();
		services.AddScoped<IEmailSender, EmailSender>();

		return services;
	}
}
