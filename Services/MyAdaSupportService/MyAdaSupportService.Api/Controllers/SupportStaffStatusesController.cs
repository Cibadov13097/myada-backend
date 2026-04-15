using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace MyAdaSupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupportStaffStatusesController : ControllerBase
{
    private readonly ISupportStaffStatusService _supportStaffStatusService;
    private readonly IConfiguration _configuration;

    public SupportStaffStatusesController(
        ISupportStaffStatusService supportStaffStatusService,
        IConfiguration configuration)
    {
        _supportStaffStatusService = supportStaffStatusService;
        _configuration = configuration;
    }

    [Authorize(Roles = "support,dispatcher,staff,admin")]
    [HttpPost]
    public async Task<ActionResult<SupportStaffStatusDto>> Create([FromBody] CreateSupportStaffStatusDto dto)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        var isAdmin = User.IsInRole("admin");

        if (dto.MemberId == Guid.Empty)
            dto.MemberId = authorizedMemberId;

        if (!isAdmin && dto.MemberId != authorizedMemberId)
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

    [Authorize(Roles = "support,dispatcher,staff,admin")]
    [HttpGet("member/{memberId:guid}")]
    public async Task<ActionResult<SupportStaffStatusDto>> GetByMemberId(Guid memberId)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        var isAdmin = User.IsInRole("admin");
        if (!isAdmin && memberId != authorizedMemberId)
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

    [AllowAnonymous]
    [HttpPost("internal/provision")]
    public async Task<ActionResult<SupportStaffStatusDto>> Provision([FromBody] CreateSupportStaffStatusDto dto)
    {
        if (!IsValidInternalProvisioningRequest())
            return Unauthorized("Invalid internal provisioning key.");

        if (dto.MemberId == Guid.Empty)
            return BadRequest("MemberId is required.");

        var effectiveStatus = string.IsNullOrWhiteSpace(dto.Status)
            ? "Offline"
            : dto.Status;

        try
        {
            var result = await _supportStaffStatusService.EnsureStatusAsync(dto.MemberId, effectiveStatus);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "support,dispatcher,staff,admin")]
    [HttpPut("member/{memberId:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid memberId, [FromBody] UpdateSupportStaffStatusDto dto)
    {
        if (!TryGetAuthorizedMemberId(out var authorizedMemberId, out var authError))
            return authError!;

        var isAdmin = User.IsInRole("admin");
        if (!isAdmin && memberId != authorizedMemberId)
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

    private bool IsValidInternalProvisioningRequest()
    {
        var expectedKey = _configuration["InternalAuth:ProvisioningKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
            return false;

        if (!Request.Headers.TryGetValue("X-Internal-Key", out var providedValues))
            return false;

        var providedKey = providedValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
