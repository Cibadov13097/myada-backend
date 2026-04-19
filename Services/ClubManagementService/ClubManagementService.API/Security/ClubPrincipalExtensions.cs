using System.Security.Claims;

namespace ClubManagementService.API.Security;

public static class ClubPrincipalExtensions
{
    /// <summary>
    /// Matches AuthService JWT role claims (<see cref="ClaimTypes.Role"/>), Identity role name <c>admin</c>.
    /// </summary>
    public static bool IsMyAdaAdministrator(this ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        if (principal.IsInRole("admin") || principal.IsInRole("Admin"))
            return true;

        foreach (var claim in principal.Claims)
        {
            var isRoleClaim = claim.Type == ClaimTypes.Role
                || string.Equals(claim.Type, "role", StringComparison.OrdinalIgnoreCase);
            if (isRoleClaim && string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
