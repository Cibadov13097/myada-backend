using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface IProfileImageStorageService
    {
        bool IsSupportedImage(string fileName);

        Task<string> UploadProfileImageAsync(
            Stream content,
            string fileName,
            string? contentType,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
