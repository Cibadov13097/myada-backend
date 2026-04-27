using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MyAdaSupportService.Api.Security;

/// <summary>Public half of the Auth seed signing key (same PEM as Auth integration tests). Used only when MYADA_TEST_JWKS_URL is set.</summary>
public static class SupportApiTestKeys
{
    public static RsaSecurityKey CreateRsaSecurityKey()
    {
        var asm = typeof(SupportApiTestKeys).Assembly;
        using var stream = asm.GetManifestResourceStream("MyAdaSupportService.Api.AuthKeys.public.pem")
            ?? throw new InvalidOperationException("Missing embedded AuthKeys.public.pem.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var pem = reader.ReadToEnd();
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(pem);
        var p = rsa.ExportParameters(false);
        return new RsaSecurityKey(p) { KeyId = "my-key-id-1" };
    }
}
