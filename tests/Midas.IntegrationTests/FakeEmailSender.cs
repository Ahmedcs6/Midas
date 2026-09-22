using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Midas.Api.Interfaces;
using Midas.Api.Models.Dtos;
namespace Midas.Tests;

public class FakeEmailSender(ILogger<FakeEmailSender> logger) : IEmailSender
{
	public readonly ConcurrentBag<IEmailJob> Sent = [];

	public void Clear() => Sent.Clear();

	public Task SendAsync(IEmailJob job, CancellationToken cancellationToken)
	{
		Sent.Add(job);
		switch (job)
		{
			case ConfirmEmailJob confirm:
				logger.LogInformation("confirm email sended to {email}", confirm.Email);
				break;

			case PasswordResetJob reset:

				logger.LogInformation("reset email sended to {email}", reset.Email);
				break;

			case SecurityAlertJob security:

				logger.LogInformation("security email sended to {email}", security.Email);
				break;

			default:
				throw new NotSupportedException(
					$"Unsupported email job: {job.GetType().Name}");
		}

		return Task.CompletedTask;
	}
}
