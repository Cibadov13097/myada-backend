using System.IO;

namespace MyAdaSupportService.Application.Abstractions;

public interface IFileStorageService
{
    bool IsSupportedImage(string fileName);

    bool IsManagedImageUrl(string? fileUrl);

    Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        string directory,
        CancellationToken cancellationToken = default);

    bool IsManagedUrl(string? fileUrl);

    Task DeleteIfManagedAsync(string? fileUrl, CancellationToken cancellationToken = default);
}
