using FluentValidation;

namespace Midas.Api.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
	public RefreshTokenRequestValidator()
	{
		RuleFor(x => x.RefreshToken)
			.NotEmpty()
			.Must(BeBase64)
			.WithMessage("RefreshToken must be a valid Base64 string.");
	}

	private static bool BeBase64(string token)
	{
		try
		{
			Convert.FromBase64String(token);
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
