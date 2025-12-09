namespace LostFoundService.Api.Security
{
    using Microsoft.IdentityModel.Tokens;
    using System.Security.Cryptography;

    public class JwksKey
    {
        public string Kty { get; set; }
        public string Use { get; set; }
        public string Alg { get; set; }
        public string Kid { get; set; }
        public string N { get; set; }
        public string E { get; set; }

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

}
