using FluentValidation;

namespace Midas.Api.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty();

		RuleFor(x => x.Token)
			.NotEmpty();

		RuleFor(x => x.NewPassword)
			.NotEmpty();
	}
}
