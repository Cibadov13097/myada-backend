using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;

namespace ClubManagementService.API.Security
{
    public static class JwksProvider
    {
        private static readonly object _lock = new();
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

            if (jwks != null)
            {
                lock (_lock)
                {
                    _keys = jwks.GetSigningKeys();
                }
            }
        }

        public static IList<SecurityKey> GetKeys()
        {
            lock (_lock)
            {
                return _keys;
            }
        }
    }
}
