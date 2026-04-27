using System.Reflection;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MyAda.Testing.JwksStub;

/// <summary>Reusable RSA (same <c>public.pem</c> as <see cref="LoopbackJwksServer"/>) for in-process test JWT validation.</summary>
public static class JwtTestKeys
{
    /// <summary>RS256 public key (kid = myada-auth) matching the Auth integration <c>private.pem</c> pair.</summary>
    public static RsaSecurityKey CreateRsaSecurityKey()
    {
        var asm = typeof(LoopbackJwksServer).Assembly;
        using var stream = asm.GetManifestResourceStream("MyAda.Testing.JwksStub.AuthKeys.public.pem")
            ?? throw new InvalidOperationException("Missing embedded MyAda.Testing.JwksStub.AuthKeys.public.pem");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var pem = reader.ReadToEnd();
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(pem);
        // Export public parameters; do not return RsaSecurityKey(rsa) after rsa is disposed.
        var p = rsa.ExportParameters(false);
        return new RsaSecurityKey(p) { KeyId = "my-key-id-1" };
    }
}
