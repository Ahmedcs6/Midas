using FluentValidation;

namespace Midas.Api.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	public LoginRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress()
			.MaximumLength(256);

		RuleFor(x => x.Password)
			.NotEmpty();

		RuleFor(x => x.Client)
			.NotNull()
			.IsInEnum();
	}
}
