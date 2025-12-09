using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Services
{
    public class TokenService
    {
        private readonly RSA _rsa;
        private readonly UserManager<ApplicationUser> _userManager;
        public TokenService(RSA rsa, UserManager<ApplicationUser> userManager)
        {
            _rsa = rsa;
            _userManager = userManager;  
        }


        public async Task<String> GenerateToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var creds = new SigningCredentials(
                new RsaSecurityKey(_rsa),
                SecurityAlgorithms.RsaSha256
            );
           
            var claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim("userType", user.UserType.ToString())
         
        };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role)); 
            }
            var token = new JwtSecurityToken(
                issuer: "myada-auth",
                audience: "myada-api",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}