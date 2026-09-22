using System.IO;
namespace Midas.Api.Validators;

public static class ImageFileRules
{
	public const long MaxFileSizeBytes = 5 * 1024 * 1024;
	public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

	public static bool BeAnImage(IFormFile? file)
	{
		if (file is null)
			return true;
		var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
		return AllowedExtensions.Contains(extension)
			|| file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
	}
}
