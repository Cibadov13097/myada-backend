using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;

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
        if (dto.MemberId <= 0)
            return BadRequest("MemberId must be greater than 0");

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

    [HttpGet("member/{memberId:int}")]
    public async Task<ActionResult<SupportStaffStatusDto>> GetByMemberId(int memberId)
    {
        try
        {
            var result = await _supportStaffStatusService.GetByMemberIdAsync(memberId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<SupportStaffStatusDto>>> GetByStatus(string status)
    {
        var result = await _supportStaffStatusService.GetByStatusAsync(status);
        return Ok(result);
    }

    [HttpPut("member/{memberId:int}")]
    public async Task<IActionResult> UpdateStatus(int memberId, [FromBody] UpdateSupportStaffStatusDto dto)
    {
        await _supportStaffStatusService.UpdateStatusAsync(memberId, dto);
        return NoContent();
    }
}
