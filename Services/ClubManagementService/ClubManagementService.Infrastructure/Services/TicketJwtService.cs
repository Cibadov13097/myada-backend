using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClubManagementService.Infrastructure.Services;

public sealed class TicketJwtService : ITicketJwtService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TokenValidationParameters _validationParameters;
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly string _audience;

    public TicketJwtService(IConfiguration config)
    {
        var key = config["Tickets:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Tickets:SigningKey must be configured and at least 32 characters.");

        _issuer = config["Tickets:Issuer"] ?? "myada-tickets";
        _audience = config["Tickets:Audience"] ?? "myada-scanner";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.Zero
        };
    }

    public string CreateTicketJwt(Guid userId, int eventId, string ticketId, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString("D")),
            new("eventId", eventId.ToString()),
            new("ticketId", ticketId)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        // iat is not emitted by default; add explicitly.
        token.Payload["iat"] = now.ToUnixTimeSeconds();
        return _handler.WriteToken(token);
    }

    public ClaimsPrincipal ValidateTicketJwt(string jwt, bool validateLifetime = true)
    {
        var p = _validationParameters.Clone();
        p.ValidateLifetime = validateLifetime;
        return _handler.ValidateToken(jwt, p, out _);
    }
}

