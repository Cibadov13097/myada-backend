using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthService.API.Controllers
{

    [ApiController]
    public class JwksController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public JwksController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("/.well-known/jwks.json")]
        public IActionResult GetJwks()
        {
            var configuredPath = _configuration["Jwt:PublicKeyPath"];
            var candidatePaths = new[]
            {
                configuredPath,
                "/keys/public.pem",
                Path.Combine(Directory.GetCurrentDirectory(), "Keys", "public.pem")
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToArray();

            var keyPath = candidatePaths.FirstOrDefault(System.IO.File.Exists);
            if (keyPath is null)
                return StatusCode(500, "JWT public key file was not found.");

            var publicKeyPem = System.IO.File.ReadAllText(keyPath);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var parameters = rsa.ExportParameters(false);

            var jwk = new
            {
                kty = "RSA",
                use = "sig",
                alg = "RS256",
                kid = _configuration["Jwt:KeyId"] ?? "my-key-id-1",
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