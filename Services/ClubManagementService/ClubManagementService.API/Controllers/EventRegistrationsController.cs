using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.API.Security;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/event-registrations")]
[ApiController]
[Tags("Event Registrations")]
public class EventRegistrationsController : ControllerBase
{
    private readonly IEventRegistrationService _service;

    public EventRegistrationsController(IEventRegistrationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("by-event/{eventId:int}")]
    public async Task<IActionResult> GetByEventId(int eventId) =>
        Ok(await _service.GetByEventIdAsync(eventId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateEventRegistrationRequest request)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var dto = new CreateEventRegistrationDto
        {
            EventId = request.EventId,
            UserId = userId
        };

        var created = await _service.CreateAsync(dto);
        // Avoid CreatedAtAction route mismatch issues (API versioning + wrappers).
        return Ok(created);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRegistrationRequest request)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var current = await _service.GetByIdAsync(id);
        if (current == null)
            return NotFound();

        if (!User.IsMyAdaAdministrator() && current.UserId != userId)
            return Forbid();

        var updated = await _service.UpdateAsync(id, new UpdateEventRegistrationDto
        {
            EventId = request.EventId
        });

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var current = await _service.GetByIdAsync(id);
        if (current == null)
            return NotFound();

        if (!User.IsMyAdaAdministrator() && current.UserId != userId)
            return Forbid();

        await _service.DeleteAsync(id);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class CreateEventRegistrationRequest
    {
        public int EventId { get; set; }
    }

    public sealed class UpdateEventRegistrationRequest
    {
        public int EventId { get; set; }
    }
}
