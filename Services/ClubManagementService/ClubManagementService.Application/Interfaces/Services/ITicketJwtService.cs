using System.Security.Claims;

namespace ClubManagementService.Application.Interfaces.Services;

public interface ITicketJwtService
{
    string CreateTicketJwt(Guid userId, int eventId, string ticketId, DateTimeOffset now, DateTimeOffset expiresAt);
    ClaimsPrincipal ValidateTicketJwt(string jwt, bool validateLifetime = true);
}

