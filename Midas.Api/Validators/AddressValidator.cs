using FluentValidation;
namespace Midas.Api.Validators;

public class AddressValidator : AbstractValidator<Address>
{
	public AddressValidator()
	{
		RuleFor(x => x.Country)
			.MaximumLength(100)
			.When(x => x.Country is not null);

		RuleFor(x => x.State)
			.MaximumLength(100)
			.When(x => x.State is not null);

		RuleFor(x => x.City)
			.MaximumLength(100)
			.When(x => x.City is not null);

		RuleFor(x => x.Street)
			.MaximumLength(200)
			.When(x => x.Street is not null);
	}
}
