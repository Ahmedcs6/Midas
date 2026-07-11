namespace Midas.Api.Interfaces;

public interface IEmailSender
{
	Task SendEmailAsync(string email, string subject, string htmlMessage);
	Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink);
	Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink);
	Task SendSecurityAlertAsync(ApplicationUser user, string email);
}
