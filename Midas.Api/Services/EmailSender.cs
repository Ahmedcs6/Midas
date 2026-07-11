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
	public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
	{
		string htmlMessage =
		$"""
    <h2>Welcome to Midas, {user.FirstName}!</h2>

    <p>Thanks for creating an account.</p>

    <p>Please confirm your email by clicking the button below:</p>

    <p>
        <a href="{confirmationLink}"
           style="display:inline-block;padding:10px 20px;background:#0ea5e9;color:#fff;text-decoration:none;border-radius:6px;">
            Confirm Email
        </a>
    </p>

    <p>If you didn't create this account, you can safely ignore this email.</p>

    <p>Thanks,<br>Midas Team</p>
    """;
		await SendEmailAsync(email, "Confirm Email", htmlMessage);
	}

	public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
	{
		string html =
		$$"""
    <h2>Hello {{user.FirstName}},</h2>

    <p>
        We received a request to reset your password.
    </p>

    <p>
        Click the button below to create a new password:
    </p>

    <p>
        <a href="{{resetLink}}"
           style="
               display:inline-block;
               padding:12px 24px;
               background-color:#0d6efd;
               color:#ffffff;
               text-decoration:none;
               border-radius:6px;
               font-weight:bold;">
            Reset Password
        </a>
    </p>

    <p>
        This link will expire soon for security reasons.
    </p>

    <p>
        If you didn't request a password reset, you can safely ignore this email.
        Your password will remain unchanged.
    </p>
    """;

		await SendEmailAsync(
			email,
			"Reset your password",
			html);
	}
	public async Task SendSecurityAlertAsync(
		ApplicationUser user,
		string email)
	{
		string html =
		$$"""
    <h2>Security Alert</h2>

    <p>Hello {{user.FirstName}},</p>

    <p>
        We detected an attempt to use a refresh token that has already been revoked.
    </p>

    <p>
        This may indicate that someone has gained access to one of your previous login sessions.
    </p>

    <p>
        If this was you, no further action is required.
    </p>

    <p>
        If you don't recognize this activity, we recommend that you:
    </p>

    <ul>
        <li>Change your password immediately.</li>
        <li>Review your active sessions and sign out of devices you don't recognize.</li>
        <li>Contact support if you believe your account has been compromised.</li>
    </ul>

    <p>
        Keeping your account secure is important to us.
    </p>

    <p>
        Midas Security Team
    </p>
    """;

		await SendEmailAsync(
			email,
			"Security Alert: Suspicious Account Activity",
			html);
	}
}

