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
