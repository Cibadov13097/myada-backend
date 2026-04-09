using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/events")]
[ApiController]
[Authorize]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventRegistrationService _registrationService;

    public EventsController(IEventService eventService, IEventRegistrationService registrationService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clubId = null, [FromQuery] bool myClubsOnly = false, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var events = await _eventService.GetAllAsync();
        var filtered = events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var total = filtered.Count();
        var items = filtered.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1));
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
        var reg = await _registrationService.CreateAsync(new CreateEventRegistrationDto
        {
            EventId = Guid.NewGuid(),
            UserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new
        {
            registrationId = $"REG-{reg.Id}",
            ticketId = $"TC-{DateTime.UtcNow.Year}-{reg.Id:D4}",
            qrToken = Guid.NewGuid().ToString("N")
        });
    }

    [HttpDelete("{eventId:int}/registrations")]
    public IActionResult CancelRegistration(int eventId) => NoContent();

    [HttpGet("{eventId:int}/ticket")]
    public IActionResult GetTicket(int eventId)
    {
        return Ok(new
        {
            ticketId = $"TC-{DateTime.UtcNow.Year}-0001",
            eventId,
            attendeeName = "Student",
            qrData = Guid.NewGuid().ToString("N")
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto dto)
    {
        var created = await _eventService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { eventId = created.Id }, created);
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
}
