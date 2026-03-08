using AuthService.Application.Options;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
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

        public TokenService(RSA rsa, UserManager<ApplicationUser> userManager, JwtOptions jwtOptions)
        {
            _rsa = rsa;
            _userManager = userManager;
            _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
        }

        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var signingKey = new RsaSecurityKey(_rsa) { KeyId = "my-key-id-1" };
            var creds = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.RsaSha256
            );

            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("firstName", user.FirstName ?? string.Empty),
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
                expires: now.AddHours(_jwtOptions.TokenLifetimeHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}