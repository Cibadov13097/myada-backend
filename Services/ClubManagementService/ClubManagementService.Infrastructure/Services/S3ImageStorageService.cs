using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.IO;

namespace ClubManagementService.Infrastructure.Services;

public sealed class S3ImageStorageService : IImageStorageService, IDisposable
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".pdf",
        ".doc",
        ".docx"
    };

    private readonly S3StorageOptions _options;
    private readonly IAmazonS3 _s3Client;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _bucketInitialized;

    public S3ImageStorageService(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.BucketName))
            throw new InvalidOperationException("S3:BucketName is required.");

        if (string.IsNullOrWhiteSpace(_options.Region))
            throw new InvalidOperationException("S3:Region is required.");

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
        };

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            config.ServiceURL = _options.ServiceUrl;
            config.ForcePathStyle = _options.ForcePathStyle;
        }

        _s3Client = CreateClient(config, _options);
    }

    public bool IsSupportedImage(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    public async Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        string directory,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedImage(fileName))
            throw new ArgumentException("Unsupported file extension.", nameof(fileName));

        await EnsureBucketReadyAsync(cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var safeDirectory = string.IsNullOrWhiteSpace(directory)
            ? "uploads"
            : directory.Trim('/').Replace("\\", "/", StringComparison.Ordinal);
        var keyPrefix = string.IsNullOrWhiteSpace(_options.KeyPrefix)
            ? string.Empty
            : $"{_options.KeyPrefix.Trim('/').Replace("\\", "/", StringComparison.Ordinal)}/";
        var objectKey = $"{keyPrefix}{safeDirectory}/{Guid.NewGuid():N}{extension}";

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = ResolveContentType(contentType, extension)
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return BuildPublicUrl(objectKey);
    }

    private static IAmazonS3 CreateClient(AmazonS3Config config, S3StorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.AccessKey) &&
            !string.IsNullOrWhiteSpace(options.SecretKey))
        {
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        }

        return new AmazonS3Client(config);
    }

    private async Task EnsureBucketReadyAsync(CancellationToken cancellationToken)
    {
        if (_bucketInitialized)
            return;

        await _bucketInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketInitialized)
                return;

            if (_options.AutoCreateBucket)
            {
                var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.BucketName);
                if (!exists)
                {
                    var bucketRequest = new PutBucketRequest
                    {
                        BucketName = _options.BucketName,
                        UseClientRegion = true
                    };

                    await _s3Client.PutBucketAsync(bucketRequest, cancellationToken);
                }
            }

            _bucketInitialized = true;
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }

    private string BuildPublicUrl(string objectKey)
    {
        var encodedKey = EncodeKey(objectKey);

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{encodedKey}";

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var serviceUri = new Uri(_options.ServiceUrl);
            var authority = $"{serviceUri.Scheme}://{serviceUri.Authority}";
            if (_options.ForcePathStyle)
                return $"{authority}/{_options.BucketName}/{encodedKey}";

            return $"{serviceUri.Scheme}://{_options.BucketName}.{serviceUri.Authority}/{encodedKey}";
        }

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{encodedKey}";
    }

    private static string EncodeKey(string objectKey)
    {
        return string.Join("/",
            objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }

    private static string ResolveContentType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
            return contentType;

        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    public void Dispose()
    {
        _bucketInitLock.Dispose();
        _s3Client.Dispose();
    }
}
