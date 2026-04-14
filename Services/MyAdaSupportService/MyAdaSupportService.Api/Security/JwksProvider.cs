using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace MyAdaSupportService.Api.Security;

public static class JwksProvider
{
    private static readonly object LockObject = new();
    private static List<SecurityKey> _keys = new();
    private static string? _jwksUrl;

    public static async Task InitializeAsync(string jwksUrl)
    {
        if (string.IsNullOrWhiteSpace(jwksUrl))
            throw new ArgumentException("JWKS URL is required.", nameof(jwksUrl));

        _jwksUrl = jwksUrl.TrimEnd('/').TrimEnd();
        if (!_jwksUrl.EndsWith("/.well-known/jwks.json", StringComparison.OrdinalIgnoreCase))
            _jwksUrl = _jwksUrl.TrimEnd('/') + "/.well-known/jwks.json";

        await RefreshAsync();
    }

    public static async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_jwksUrl))
            return;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await client.GetStringAsync(_jwksUrl);
            var document = JsonSerializer.Deserialize<JwksDocument>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var keys = document?.Keys?
                .Where(k => string.Equals(k.Kty, "RSA", StringComparison.OrdinalIgnoreCase))
                .Select(k => k.ToSecurityKey())
                .ToList() ?? new List<SecurityKey>();

            lock (LockObject)
            {
                _keys = keys;
            }
        }
        catch
        {
            // Keep existing keys on transient failures.
        }
    }

    public static IEnumerable<SecurityKey> GetKeys()
    {
        lock (LockObject)
        {
            return _keys.ToList();
        }
    }
}
