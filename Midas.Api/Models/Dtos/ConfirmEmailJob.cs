namespace Midas.Api.Models.Dtos;

public sealed record ConfirmEmailJob(ApplicationUser User, string Email, string ConfirmationLink) : IEmailJob;
