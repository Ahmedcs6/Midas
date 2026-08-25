namespace Midas.Api.Models.Dtos;

public sealed record PasswordResetJob(ApplicationUser User, string Email, string ResetLink) : IEmailJob;
