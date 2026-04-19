using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/events")]
[ApiController]
[Authorize]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventRegistrationService _registrationService;
    private readonly IClubMembershipRepository _clubMembershipRepository;

    public EventsController(
        IEventService eventService,
        IEventRegistrationService registrationService,
        IClubMembershipRepository clubMembershipRepository)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _clubMembershipRepository = clubMembershipRepository;
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

        if (myClubsOnly)
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

        var existing = await _registrationService.GetByEventAndUserAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (existing != null)
            return Conflict(new { message = "Already registered for this event." });

        var current = (await _registrationService.GetByEventIdAsync(eventId)).Count;
        if (evt.SeatLimit > 0 && current >= evt.SeatLimit)
            return Conflict(new { message = "This event has reached its seat limit." });

        var reg = await _registrationService.CreateAsync(new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId
        });

        return Ok(new
        {
            registrationId = $"REG-{reg.Id}",
            ticketId = $"TC-{DateTime.UtcNow.Year}-{reg.Id:D4}",
            qrToken = Guid.NewGuid().ToString("N")
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

        return NoContent();
    }

    [HttpGet("{eventId:int}/ticket")]
    public async Task<IActionResult> GetTicket(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var reg = await _registrationService.GetByEventAndUserAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (reg == null)
            return NotFound();

        return Ok(new
        {
            ticketId = $"TC-{DateTime.UtcNow.Year}-{reg.Id:D4}",
            eventId,
            attendeeName = "Student",
            qrData = Guid.NewGuid().ToString("N")
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
}
