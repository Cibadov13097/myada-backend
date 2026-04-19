using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace CallService.Api.Realtime;

public sealed class CoturnIceConfigurationProvider : IIceConfigurationProvider
{
    private readonly TurnOptions _options;
    private readonly ILogger<CoturnIceConfigurationProvider> _logger;

    public CoturnIceConfigurationProvider(
        IOptions<TurnOptions> options,
        ILogger<CoturnIceConfigurationProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IceConfigurationDto GetForUser(string userId)
    {
        var normalizedUserId = userId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUserId))
            throw new ArgumentException("userId is required.", nameof(userId));

        var generatedAtUtc = DateTime.UtcNow;

        if (!_options.Enabled)
            return new IceConfigurationDto(generatedAtUtc, generatedAtUtc, Array.Empty<IceServerDto>());

        var urls = _options.Urls
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (urls.Length == 0)
        {
            _logger.LogWarning("TURN is enabled but Turn:Urls is empty. Returning no ICE servers.");
            return new IceConfigurationDto(generatedAtUtc, generatedAtUtc, Array.Empty<IceServerDto>());
        }

        if (string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            _logger.LogWarning("TURN is enabled but Turn:SharedSecret is missing. Returning no ICE servers.");
            return new IceConfigurationDto(generatedAtUtc, generatedAtUtc, Array.Empty<IceServerDto>());
        }

        var ttlSeconds = NormalizeTtlSeconds(_options.CredentialTtlSeconds);
        var expiresAt = new DateTimeOffset(generatedAtUtc).AddSeconds(ttlSeconds);
        var username = $"{expiresAt.ToUnixTimeSeconds()}:{normalizedUserId}";
        var credential = BuildCredential(username, _options.SharedSecret);

        return new IceConfigurationDto(
            generatedAtUtc,
            expiresAt.UtcDateTime,
            new[]
            {
                new IceServerDto(urls, username, credential)
            });
    }

    private static int NormalizeTtlSeconds(int configuredTtl)
    {
        if (configuredTtl < 60)
            return 60;

        if (configuredTtl > 86400)
            return 86400;

        return configuredTtl;
    }

    private static string BuildCredential(string username, string sharedSecret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(sharedSecret.Trim());
        var usernameBytes = Encoding.UTF8.GetBytes(username);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(usernameBytes);
        return Convert.ToBase64String(hash);
    }
}
