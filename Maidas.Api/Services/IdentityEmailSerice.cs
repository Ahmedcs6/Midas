using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Maidas.Api.Services;

public class IdentityEmailSerice(UserManager<ApplicationUser> userManager, IEmailSender emailSender) : IIdentityEmailService
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
<h2>Welcome to Maidas</h2>

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
}
