using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface IImageStorageService
    {
        bool IsSupportedImage(string fileName);

        Task<string> UploadImageAsync(
            Stream content,
            string fileName,
            string? contentType,
            string directory,
            CancellationToken cancellationToken = default);
    }
}
