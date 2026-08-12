using System.Threading;

namespace Midas.Api.Interfaces;

public interface IFileStorage
{
	Task<string> SaveAsync(
			IFormFile file,
			string folder,
			CancellationToken cancellationToken = default);
	Task DeleteAsync(
			string filePath,
			CancellationToken cancellationToken = default);
}
