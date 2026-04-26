using System.IO;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IImageStorageService
{
    bool IsSupportedImage(string fileName);

    Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        string directory,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
