using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthService.API.Controllers
{

    [ApiController]
    public class JwksController : ControllerBase
    {
        [HttpGet("/.well-known/jwks.json")]
        public IActionResult GetJwks()
        {
            var basePath = Directory.GetCurrentDirectory();
            var keyPath = Path.Combine(basePath, "Keys", "public.pem");

            var publicKeyPem = System.IO.File.ReadAllText(keyPath);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var parameters = rsa.ExportParameters(false);

            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = "my-key-id-1",
                n = Base64UrlEncoder.Encode(parameters.Modulus),
                e = Base64UrlEncoder.Encode(parameters.Exponent)
            };

            var jwks = new
            {
                keys = new[] { jwk }
            };

            return Ok(jwks);
        }
    }
}