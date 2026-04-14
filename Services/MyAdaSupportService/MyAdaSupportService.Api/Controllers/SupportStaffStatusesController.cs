using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;
using System.Security.Claims;

namespace MyAdaSupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupportStaffStatusesController : ControllerBase
{
    private readonly ISupportStaffStatusService _supportStaffStatusService;

    public SupportStaffStatusesController(ISupportStaffStatusService supportStaffStatusService)
    {
        _supportStaffStatusService = supportStaffStatusService;
    }

    [Authorize(Roles = "support,dispatcher,staff")]
    [HttpPost]
    public async Task<ActionResult<SupportStaffStatusDto>> Create([FromBody] CreateSupportStaffStatusDto dto)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        if (dto.MemberId == Guid.Empty)
            dto.MemberId = authorizedMemberId;

        if (dto.MemberId != authorizedMemberId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest("Status is required");

        try
        {
            var result = await _supportStaffStatusService.CreateStatusAsync(dto);
            return CreatedAtAction(nameof(GetByMemberId), new { memberId = result.MemberId }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "support,dispatcher,staff")]
    [HttpGet("member/{memberId:guid}")]
    public async Task<ActionResult<SupportStaffStatusDto>> GetByMemberId(Guid memberId)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        if (memberId != authorizedMemberId)
            return Forbid();

        var result = await _supportStaffStatusService.GetOrCreateByMemberIdAsync(memberId);
        return Ok(result);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<SupportStaffStatusDto>>> GetByStatus(string status)
    {
        var result = await _supportStaffStatusService.GetByStatusAsync(status);
        return Ok(result);
    }

    [Authorize(Roles = "support,dispatcher,staff")]
    [HttpPut("member/{memberId:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid memberId, [FromBody] UpdateSupportStaffStatusDto dto)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        if (memberId != authorizedMemberId)
            return Forbid();

        await _supportStaffStatusService.UpdateStatusAsync(memberId, dto);
        return NoContent();
    }

    private bool TryGetAuthorizedMemberId(out Guid memberId, out ObjectResult? error)
    {
        memberId = Guid.Empty;
        error = null;

        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub))
        {
            error = new UnauthorizedObjectResult("Authenticated user id claim is missing.");
            return false;
        }

        if (!Guid.TryParse(sub, out memberId))
        {
            error = new BadRequestObjectResult("Authenticated user id must be a valid GUID.");
            return false;
        }

        return true;
    }
}
