using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace MyAdaSupportService.Api.Security;

public class JwksKey
{
    public string Kty { get; set; } = string.Empty;
    public string Use { get; set; } = string.Empty;
    public string Alg { get; set; } = string.Empty;
    public string Kid { get; set; } = string.Empty;
    public string N { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;

    public SecurityKey ToSecurityKey()
    {
        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlEncoder.DecodeBytes(N),
            Exponent = Base64UrlEncoder.DecodeBytes(E)
        });

        return new RsaSecurityKey(rsa)
        {
            KeyId = Kid
        };
    }
}
