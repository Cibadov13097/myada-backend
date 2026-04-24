using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Infrastructure.Options;

namespace MyAdaSupportService.Infrastructure.Services;

public sealed class S3FileStorageService : IFileStorageService, IDisposable
{
    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif"
    };

    private readonly S3StorageOptions _options;
    private readonly IAmazonS3 _s3Client;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _bucketInitialized;

    public S3FileStorageService(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("S3:BucketName is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.Region))
        {
            throw new InvalidOperationException("S3:Region is required.");
        }

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
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.ContainsKey(extension);
    }

    public bool IsManagedImageUrl(string? fileUrl)
    {
        return TryExtractObjectKey(fileUrl, out var objectKey) && IsSupportedImage(objectKey);
    }

    public async Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        string directory,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedImage(fileName))
        {
            throw new ArgumentException("Only JPG, JPEG, PNG, WEBP, and GIF files are supported.", nameof(fileName));
        }

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

    public bool IsManagedUrl(string? fileUrl)
    {
        return TryExtractObjectKey(fileUrl, out _);
    }

    public async Task DeleteIfManagedAsync(string? fileUrl, CancellationToken cancellationToken = default)
    {
        if (!TryExtractObjectKey(fileUrl, out var objectKey))
        {
            return;
        }

        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        }, cancellationToken);
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
        {
            return;
        }

        await _bucketInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            if (_options.AutoCreateBucket)
            {
                var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.BucketName);
                if (!exists)
                {
                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _options.BucketName,
                        UseClientRegion = true
                    }, cancellationToken);
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
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{encodedKey}";
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var serviceUri = new Uri(_options.ServiceUrl);
            var authority = $"{serviceUri.Scheme}://{serviceUri.Authority}";
            if (_options.ForcePathStyle)
            {
                return $"{authority}/{_options.BucketName}/{encodedKey}";
            }

            return $"{serviceUri.Scheme}://{_options.BucketName}.{serviceUri.Authority}/{encodedKey}";
        }

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{encodedKey}";
    }

    private bool TryExtractObjectKey(string? fileUrl, out string objectKey)
    {
        objectKey = string.Empty;
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return false;
        }

        foreach (var prefix in GetManagedPrefixes())
        {
            if (fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                objectKey = DecodeKey(fileUrl[prefix.Length..]);
                return !string.IsNullOrWhiteSpace(objectKey);
            }
        }

        return false;
    }

    private IEnumerable<string> GetManagedPrefixes()
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            yield return $"{_options.PublicBaseUrl.TrimEnd('/')}/";
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var serviceUri = new Uri(_options.ServiceUrl);
            var authority = $"{serviceUri.Scheme}://{serviceUri.Authority}";
            yield return $"{authority}/{_options.BucketName}/";
            yield return $"{serviceUri.Scheme}://{_options.BucketName}.{serviceUri.Authority}/";
        }

        yield return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/";
    }

    private static string EncodeKey(string objectKey)
    {
        return string.Join("/",
            objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }

    private static string DecodeKey(string encodedKey)
    {
        return string.Join("/",
            encodedKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString));
    }

    private static string ResolveContentType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return AllowedExtensions.TryGetValue(extension, out var resolved)
            ? resolved
            : "application/octet-stream";
    }

    public void Dispose()
    {
        _bucketInitLock.Dispose();
        _s3Client.Dispose();
    }
}
