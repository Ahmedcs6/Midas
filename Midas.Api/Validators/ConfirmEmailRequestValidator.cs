using FluentValidation;

namespace Midas.Api.Validators;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
	public ConfirmEmailRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress()
			.MaximumLength(256);
	}
}
