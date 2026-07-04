namespace Maidas.Api.Interfaces;

public interface IIdentityEmailService
{
	Task SendConfirmationEmailAsync(ApplicationUser user);
	Task SecurityAlert(string email);
}
