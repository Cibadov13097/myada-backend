using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.API.Security;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/events")]
[Route("api/events")]
[ApiController]
[Authorize]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventRegistrationService _registrationService;
    private readonly IClubMembershipRepository _clubMembershipRepository;
    private readonly IClubNotificationService _notificationService;

    public EventsController(
        IEventService eventService,
        IEventRegistrationService registrationService,
        IClubMembershipRepository clubMembershipRepository,
        IClubNotificationService notificationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _clubMembershipRepository = clubMembershipRepository;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? clubId = null,
        [FromQuery] bool myClubsOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 0)
    {
        if (limit <= 0)
            limit = GetDefaultEventPageSize();

        var events = await _eventService.GetAllAsync();
        var filtered = events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (clubId is > 0)
            filtered = filtered.Where(e => e.ClubId == clubId);

        if (myClubsOnly && !User.IsMyAdaAdministrator())
        {
            var userId = GetCurrentUserIdOrDefault();
            if (userId == Guid.Empty)
                return Unauthorized();

            var myClubIds = await _clubMembershipRepository.GetClubIdsByUserAsync(userId, HttpContext.RequestAborted).ConfigureAwait(false);
            if (myClubIds.Count == 0)
            {
                return Ok(new { items = Array.Empty<object>(), total = 0, page, limit });
            }

            var set = myClubIds.ToHashSet();
            filtered = filtered.Where(e => set.Contains(e.ClubId));
        }

        var total = filtered.Count();
        var items = filtered
            .Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1))
            .Take(Math.Max(limit, 1));
        return Ok(new { items, total, page, limit });
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> GetById(int eventId)
    {
        var dto = await _eventService.GetByIdAsync(eventId);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("{eventId:int}/registrations")]
    public async Task<IActionResult> Register(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var evt = await _eventService.GetByIdAsync(eventId);
        if (evt == null)
            return NotFound();

        EventRegistrationDto reg;
        try
        {
            reg = await _registrationService.RegisterAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { message = "This event has reached its seat limit." });
        }

        var registeredCount = (await _registrationService.GetByEventIdAsync(eventId)).Count;

        return Ok(new
        {
            ticketId = reg.TicketId,
            eventId = eventId.ToString(),
            userId = userId.ToString("D"),
            jwt = reg.RegistrationJwt,
            attendeeId = userId.ToString("D"),
            @event = new
            {
                id = evt.Id.ToString(),
                name = evt.Name,
                date = evt.StartTime.ToString("yyyy-MM-dd"),
                startTime = evt.StartTime.ToString("HH:mm"),
                endTime = evt.EndTime.ToString("HH:mm"),
                location = evt.Location,
                clubName = (string?)null,
                imageUrl = evt.ImageUrl,
                seatLimit = evt.SeatLimit,
                registeredCount
            }
        });
    }

    [HttpDelete("{eventId:int}/registrations")]
    public async Task<IActionResult> CancelRegistration(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _registrationService.DeleteByEventAndUserAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration Cancelled",
            Message = $"A registration for event '{eventId}' was cancelled.",
            Source = "club-management",
            Action = "event.registration.cancelled",
            EntityType = "event",
            EntityId = eventId.ToString(),
            ActorUserId = userId
        });

        return NoContent();
    }

    [HttpGet("{eventId:int}/ticket")]
    public async Task<IActionResult> GetTicket(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var reg = await _registrationService.GetTicketAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (reg == null)
            return NotFound();

        return Ok(new
        {
            ticketId = reg.TicketId,
            eventId = eventId.ToString(),
            userId = userId.ToString("D"),
            jwt = reg.RegistrationJwt,
            attendeeId = userId.ToString("D"),
            eventImageUrl = reg.EventImageUrlSnapshot
        });
    }

    [HttpPost("{eventId:int}/check-in")]
    [Authorize(Roles = "admin,Admin,staff,Staff")]
    public async Task<IActionResult> CheckIn(int eventId, [FromBody] CheckInRequest body, CancellationToken cancellationToken)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Jwt))
            return BadRequest(new { message = "jwt is required." });

        var staffId = GetCurrentUserIdOrDefault();
        if (staffId == Guid.Empty)
            return Unauthorized();

        var result = await _registrationService.CheckInAsync(eventId, body.Jwt, staffId, cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                status = result.Status,
                message = result.Message ?? "Entry allowed",
                ticketId = result.TicketId,
                attendeeId = result.AttendeeId.ToString("D"),
                eventId = result.EventId.ToString(),
                checkedInAt = result.CheckedInAt?.ToString("O")
            });
        }

        return Ok(new
        {
            success = false,
            status = result.Status,
            message = result.Message,
            ticketId = result.TicketId,
            attendeeId = result.AttendeeId == Guid.Empty ? null : result.AttendeeId.ToString("D"),
            eventId = result.EventId.ToString(),
            checkedInAt = result.CheckedInAt?.ToString("O")
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        var created = await _eventService.CreateAsync(dto);
        var v = HttpContext.GetClubApiMajorVersion();
        return CreatedAtAction(nameof(GetById), new { version = v, eventId = created.Id }, created);
    }

    [HttpPatch("{eventId:int}")]
    public async Task<IActionResult> Update(int eventId, [FromBody] UpdateEventDto dto)
    {
        var updated = await _eventService.UpdateAsync(eventId, dto);
        return Ok(updated);
    }

    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> Delete(int eventId)
    {
        await _eventService.DeleteAsync(eventId);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private int GetDefaultEventPageSize() => HttpContext.GetClubApiMajorVersion() >= 2 ? 24 : 12;

    public sealed class CheckInRequest
    {
        public string Jwt { get; set; } = string.Empty;
    }
}
