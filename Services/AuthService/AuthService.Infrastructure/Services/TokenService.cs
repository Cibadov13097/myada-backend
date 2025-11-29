using AuthService.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class TokenService
{
    private readonly RSA _rsa;

    public TokenService(RSA rsa)
    {
        _rsa = rsa;
    }

    public string GenerateToken(ApplicationUser user)
    {
        var creds = new SigningCredentials(
            new RsaSecurityKey(_rsa),
            SecurityAlgorithms.RsaSha256
        );

        var claims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim("userType", user.UserType.ToString()),
            new Claim("status", user.Status.ToString())
        };

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
