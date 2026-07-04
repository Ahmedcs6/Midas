using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
namespace Midas.Api.Services;

public class EmailSender(IOptions<EmailSettings> options) : IEmailSender
{
	private readonly EmailSettings _settings = options.Value;

	public async Task SendEmailAsync(string email, string subject, string htmlMessage)
	{
		var message = new MimeMessage();

		message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

		message.To.Add(MailboxAddress.Parse(email));

		message.Subject = subject;

		message.Body = new TextPart("html")
		{
			Text = htmlMessage
		};

		using var client = new SmtpClient();

		await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

		await client.AuthenticateAsync(_settings.Username, _settings.Password);

		await client.SendAsync(message);

		await client.DisconnectAsync(true);
	}
}

