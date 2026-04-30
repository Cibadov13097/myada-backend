using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Api.Realtime;

public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        return user?.FindFirst("sub")?.Value
               ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
