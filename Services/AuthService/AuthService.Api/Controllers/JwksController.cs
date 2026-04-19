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
            var candidatePaths = BuildPublicKeyCandidatePaths().ToArray();
            var keyPath = candidatePaths.FirstOrDefault(System.IO.File.Exists);
            if (keyPath is null)
            {
                var detail = candidatePaths.Length > 0
                    ? "Tried: " + string.Join("; ", candidatePaths)
                    : "No candidate paths were built (check Jwt:PublicKeyPath / Jwt:PrivateKeyPath).";
                return StatusCode(500, "JWT public key file was not found. " + detail);
            }

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

        /// <summary>Resolves PEM path for JWKS; same directory as <c>Jwt:PrivateKeyPath</c> is tried when public path is not set.</summary>
        private IEnumerable<string> BuildPublicKeyCandidatePaths()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<string>();
            void TryAdd(string? path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                try
                {
                    var full = Path.GetFullPath(path.Trim());
                    if (seen.Add(full))
                        list.Add(full);
                }
                catch (ArgumentException)
                {
                }
            }

            TryAdd(_configuration["Jwt:PublicKeyPath"]);

            var privatePath = _configuration["Jwt:PrivateKeyPath"];
            if (!string.IsNullOrWhiteSpace(privatePath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(privatePath.Trim()));
                    if (!string.IsNullOrEmpty(dir))
                        TryAdd(Path.Combine(dir, "public.pem"));
                }
                catch (ArgumentException)
                {
                }
            }

            TryAdd("/keys/public.pem");
            TryAdd(Path.Combine(Directory.GetCurrentDirectory(), "Keys", "public.pem"));
            TryAdd(Path.Combine(AppContext.BaseDirectory, "keys", "public.pem"));

            return list;
        }
    }
}