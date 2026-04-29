using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;

namespace NotificationService.Api.Security;

public static class JwksProvider
{
    private static readonly object LockObject = new();
    private static IList<SecurityKey> _keys = Array.Empty<SecurityKey>();
    private static string _jwksUrl = string.Empty;

    public static async Task InitializeAsync(string jwksUrl)
    {
        _jwksUrl = jwksUrl;
        await RefreshKeysAsync();
    }

    public static async Task RefreshKeysAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var jwks = await http.GetFromJsonAsync<JsonWebKeySet>(_jwksUrl);

        if (jwks is null)
            return;

        lock (LockObject)
        {
            _keys = jwks.GetSigningKeys();
        }
    }

    public static IList<SecurityKey> GetKeys()
    {
        lock (LockObject)
        {
            return _keys;
        }
    }
}
