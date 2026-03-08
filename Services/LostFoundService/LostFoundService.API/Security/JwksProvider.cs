using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace LostFoundService.Api.Security;

/// <summary>
/// Fetches and caches signing keys from AuthService JWKS endpoint.
/// Supports automatic key rotation via periodic refresh (no restart required).
/// </summary>
public static class JwksProvider
{
    private static readonly object _lock = new();
    private static List<SecurityKey> _keys = new();
    private static string? _jwksUrl;

    /// <summary>
    /// Initialize and fetch keys from JWKS endpoint. Call at startup.
    /// </summary>
    public static async Task InitializeAsync(string jwksUrl)
    {
        if (string.IsNullOrWhiteSpace(jwksUrl))
            throw new ArgumentException("JWKS URL is required.", nameof(jwksUrl));
        _jwksUrl = jwksUrl.TrimEnd('/').TrimEnd();
        if (!_jwksUrl.EndsWith("/.well-known/jwks.json", StringComparison.OrdinalIgnoreCase))
            _jwksUrl = _jwksUrl.TrimEnd('/') + "/.well-known/jwks.json";
        await RefreshAsync();
        if (_keys.Count == 0)
            throw new InvalidOperationException(
                $"No signing keys loaded from JWKS. Check that Auth:JwksUrl is correct and AuthService is running. URL: {_jwksUrl}");
    }

    /// <summary>
    /// Refresh keys from JWKS endpoint (for key rotation). Thread-safe.
    /// </summary>
    public static async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_jwksUrl))
            return;
        try
        {
            using var handler = new HttpClientHandler();
            if (_jwksUrl?.Contains("localhost", StringComparison.OrdinalIgnoreCase) == true)
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);
            var json = await client.GetStringAsync(_jwksUrl);
            var doc = JsonSerializer.Deserialize<JwksDocument>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var keys = doc?.Keys?
                .Where(k => string.Equals(k.Kty, "RSA", StringComparison.OrdinalIgnoreCase))
                .Select(k => k.ToSecurityKey())
                .ToList() ?? new List<SecurityKey>();
            lock (_lock)
            {
                _keys = keys;
            }
        }
        catch (Exception)
        {
            // Keep existing keys on transient failure
        }
    }

    /// <summary>
    /// Returns cached signing keys for JWT validation (RS256).
    /// </summary>
    public static IEnumerable<SecurityKey> GetKeys()
    {
        lock (_lock)
        {
            return _keys.ToList();
        }
    }
}
