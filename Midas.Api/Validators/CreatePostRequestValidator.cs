using FluentValidation;

namespace Midas.Api.Validators;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
	public CreatePostRequestValidator()
	{
		RuleFor(x => x.Content)
			.MaximumLength(5000)
			.When(x => x.Content is not null);

		RuleFor(x => x.Privacy)
			.IsInEnum();

		RuleFor(x => x.Image)
			.Must(f => f!.Length <= ImageFileRules.MaxFileSizeBytes)
			.WithMessage("Image must not exceed 5 MB.")
			.Must(ImageFileRules.BeAnImage)
			.WithMessage("Image must be a valid image file (jpg, png, webp, gif).")
			.When(x => x.Image is not null);
	}
}
