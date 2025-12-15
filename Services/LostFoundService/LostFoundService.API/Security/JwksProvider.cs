namespace LostFoundService.Api.Security
{
    using Microsoft.IdentityModel.Tokens;
    using System.Text.Json;

    public static class JwksProvider
    {
        private static List<SecurityKey> _keys;

        public static async Task InitializeAsync(string jwksUrl)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(jwksUrl);

            var doc = JsonSerializer.Deserialize<JwksDocument>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _keys = doc.Keys.Select(k => k.ToSecurityKey()).ToList();
        }

        public static IEnumerable<SecurityKey> GetKeys()
        {
            return _keys;
        }
    }

}
