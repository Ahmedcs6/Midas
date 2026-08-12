using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace Midas.Api.Services;

public class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
	public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
	{
		var physicalPath = Path.Combine(
				  environment.WebRootPath,
				  filePath);

		if (File.Exists(physicalPath))
			File.Delete(physicalPath);

		return Task.CompletedTask;
	}

	public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
	{
		var extension = Path.GetExtension(file.FileName);
		var fileName = $"{Guid.NewGuid()}{extension}";
		var directory = Path.Combine(environment.WebRootPath,
				folder);
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, fileName);
		using var stream = new FileStream(path, FileMode.Create);
		await file.CopyToAsync(stream, cancellationToken);
		return fileName;
	}
}
