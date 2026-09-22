using FluentValidation;

namespace Midas.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator()
	{
		RuleFor(x => x.FirstName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.LastName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.Gender)
			.NotNull()
			.IsInEnum();

		RuleFor(x => x.UserName)
			.NotEmpty()
			.MinimumLength(3)
			.MaximumLength(32)
			.Matches("^[a-zA-Z0-9_.@-]+$")
			.WithMessage("UserName may only contain letters, digits and _ . @ - characters.");

		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress()
			.MaximumLength(256);

		RuleFor(x => x.Password)
			.NotEmpty();
	}
}
