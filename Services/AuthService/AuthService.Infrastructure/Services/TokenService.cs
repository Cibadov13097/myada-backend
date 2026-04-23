using AuthService.Application.Options;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Services
{
    public class TokenService
    {
        private readonly RSA _rsa;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _jwtOptions;
        private readonly IConfiguration _configuration;

        public TokenService(
            RSA rsa,
            UserManager<ApplicationUser> userManager,
            JwtOptions jwtOptions,
            IConfiguration configuration)
        {
            _rsa = rsa;
            _userManager = userManager;
            _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
            _configuration = configuration;
        }

        public async Task<string> GenerateToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var rsaKey = new RsaSecurityKey(_rsa)
            {
                KeyId = _configuration["Jwt:KeyId"]
            };

            var creds = new SigningCredentials(
                rsaKey,
                SecurityAlgorithms.RsaSha256
            );

            var now = DateTime.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("firstName", user.FirstName ?? string.Empty),
                new Claim("organizationalId", user.OrganizationalId ?? string.Empty),
                new Claim("userType", user.UserType.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }
    }
}
