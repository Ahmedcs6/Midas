namespace Midas.Api.Models.Dtos;

public sealed record SecurityAlertJob(ApplicationUser User, string Email) : IEmailJob;
