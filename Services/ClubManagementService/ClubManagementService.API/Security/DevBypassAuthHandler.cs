using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClubManagementService.API.Security;

public sealed class DevBypassAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevBypassAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Use a stable, non-empty GUID so user-scoped endpoints work in Development
        // when JWT bearer validation is bypassed (e.g. AuthService/JWKS is not reachable yet).
        var devUserId = new Guid("00000000-0000-0000-0000-000000000001");

        var claims = new List<Claim>
        {
            new("sub", devUserId.ToString("D")),
            new(ClaimTypes.Name, "dev-user"),
            // Allow admin-only endpoints in Development without a real AuthService token.
            new(ClaimTypes.Role, "admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
