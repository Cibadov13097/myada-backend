using Asp.Versioning;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users/me")]
[Route("api/users/me")]
[ApiController]
[Authorize]
[Tags("Users")]
public sealed class UserMeController : ControllerBase
{
    private readonly IEventRegistrationService _registrations;

    public UserMeController(IEventRegistrationService registrations)
    {
        _registrations = registrations;
    }

    [HttpGet("event-registrations")]
    public async Task<IActionResult> GetMyEventRegistrations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var list = await _registrations.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var countsByEvent = new Dictionary<int, int>();
        foreach (var eid in list.Select(r => r.EventId).Distinct())
        {
            countsByEvent[eid] = (await _registrations.GetByEventIdAsync(eid).ConfigureAwait(false)).Count;
        }

        var items = list.Select(r => new
        {
            eventId = r.EventId.ToString(),
            ticketId = r.TicketId,
            registeredAt = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc).ToString("O"),
            @event = new
            {
                id = r.EventId.ToString(),
                name = r.EventNameSnapshot,
                imageUrl = r.EventImageUrlSnapshot,
                startTime = r.EventStartTimeSnapshot.ToString("HH:mm"),
                endTime = r.EventEndTimeSnapshot.ToString("HH:mm"),
                location = r.EventLocationSnapshot,
                seatLimit = r.EventSeatLimitSnapshot,
                registeredCount = countsByEvent.TryGetValue(r.EventId, out var c) ? c : 0
            }
        });

        return Ok(new { items });
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }
}

