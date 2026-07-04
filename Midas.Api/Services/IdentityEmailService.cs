using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Midas.Api.Services;

public class IdentityEmailService(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : IIdentityEmailService
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly IEmailSender _emailSender = emailSender;


	public async Task SendConfirmationEmailAsync(ApplicationUser user)
	{
		string token =
			await _userManager.GenerateEmailConfirmationTokenAsync(user);
		string encodedToken =
	WebEncoders.Base64UrlEncode(
		Encoding.UTF8.GetBytes(token));

		string confirmationLink =
			$"https://your-site.com/confirm-email?userId={user.Id}&token={encodedToken}";
		string htmlMessage =
		$"""
<h2>Welcome to Midas</h2>

<p>
    Please confirm your email by clicking the button below.
</p>

<p>
    <a href="{confirmationLink}">
        Confirm Email
    </a>
</p>
""";
		await _emailSender.SendEmailAsync(user.Email!, "Confirm Email", htmlMessage);
	}
	public async Task SecurityAlert(string email)
	{
		string htmlBody = """
<h2>Security Alert</h2>

<p>Hello,</p>

<p>
We detected an attempt to use a refresh token that had already been invalidated
for your account.
</p>

<p>
As a precaution, we have terminated the affected session.
</p>

<p><strong>If this wasn't you:</strong></p>

<ul>
    <li>Sign in to your account again.</li>
    <li>Change your password.</li>
    <li>Review your active devices.</li>
</ul>

<p>If you recognize this activity, you can safely ignore this email.</p>

<br>

<p>Regards,<br><strong>Midas Team</strong></p>
""";
		await _emailSender.SendEmailAsync(email, "Security Alert", htmlBody);
	}
}
