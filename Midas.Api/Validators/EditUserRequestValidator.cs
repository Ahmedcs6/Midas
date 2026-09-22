using FluentValidation;

namespace Midas.Api.Validators;

public class EditUserRequestValidator : AbstractValidator<EditUserRequest>
{
	public EditUserRequestValidator()
	{
		RuleFor(x => x.FirstName)
			.NotEmpty()
			.MaximumLength(50)
			.When(x => x.FirstName is not null);

		RuleFor(x => x.LastName)
			.NotEmpty()
			.MaximumLength(50)
			.When(x => x.LastName is not null);

		RuleFor(x => x.BirthDate)
			.Must(BeAValidPastDate)
			.WithMessage("BirthDate must be a date in the past.")
			.When(x => x.BirthDate is not null);

		RuleFor(x => x.About)
			.MaximumLength(500)
			.When(x => x.About is not null);

		RuleFor(x => x.Address!)
			.SetValidator(new AddressValidator())
			.When(x => x.Address is not null);
	}

	private static bool BeAValidPastDate(DateOnly? birthDate)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		return birthDate < today && birthDate > today.AddYears(-150);
	}
}
