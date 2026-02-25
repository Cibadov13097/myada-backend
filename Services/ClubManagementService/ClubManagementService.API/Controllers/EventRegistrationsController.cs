using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/event-registrations")]
[ApiController]
[Tags("Event Registrations")]
public class EventRegistrationsController : ControllerBase
{
    private readonly IEventRegistrationService _service;

    public EventRegistrationsController(IEventRegistrationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("by-event/{eventId:guid}")]
    public async Task<IActionResult> GetByEventId(Guid eventId) =>
        Ok(await _service.GetByEventIdAsync(eventId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRegistrationDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
