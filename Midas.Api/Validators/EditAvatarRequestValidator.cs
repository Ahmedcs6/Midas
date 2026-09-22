using FluentValidation;
namespace Midas.Api.Validators;

public class EditAvatarRequestValidator : AbstractValidator<EditAvatarRequest>
{
	public EditAvatarRequestValidator()
	{
		RuleFor(x => x.Image)
			.NotNull()
			.Must(f => f.Length <= ImageFileRules.MaxFileSizeBytes)
			.WithMessage("Image must not exceed 5 MB.")
			.Must(ImageFileRules.BeAnImage)
			.WithMessage("Image must be a valid image file (jpg, png, webp, gif).");
	}
}
