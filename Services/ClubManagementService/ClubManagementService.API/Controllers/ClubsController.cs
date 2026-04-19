using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/clubs")]
[ApiController]
[Authorize]
[Tags("Clubs")]
public class ClubsController : ControllerBase
{
    private readonly IClubService _service;

    public ClubsController(IClubService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var clubs = await _service.GetAllAsync();
        var filtered = clubs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(c => string.Equals(c.CategoryName, category, StringComparison.OrdinalIgnoreCase));

        var total = filtered.Count();
        var items = filtered.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1)).Select(c => new
        {
            id = c.Id,
            name = c.Name,
            category = c.CategoryName ?? "General",
            image = "/clubs/default.png",
            tags = Array.Empty<string>(),
            members = 0,
            about = c.Description,
            status = c.Status.ToString()
        });

        return Ok(new { items, total, page, limit });
    }

    [HttpGet("{clubId:int}")]
    public async Task<IActionResult> GetById(int clubId)
    {
        var dto = await _service.GetByIdAsync(clubId);
        if (dto == null)
            return NotFound();

        return Ok(new
        {
            id = dto.Id,
            name = dto.Name,
            category = dto.CategoryName ?? "General",
            about = dto.Description,
            focusAreas = Array.Empty<object>(),
            officers = Array.Empty<object>(),
            memberProfiles = Array.Empty<object>(),
            announcements = Array.Empty<object>(),
            email = "club@ada.edu.az",
            website = string.Empty,
            instagram = string.Empty,
            x = string.Empty,
            tiktok = string.Empty
        });
    }

    [HttpGet("{clubId:int}/members")]
    public IActionResult GetMembers(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("{clubId:int}/join-applications")]
    [Consumes("multipart/form-data")]
    public IActionResult JoinApplication(int clubId, [FromForm] JoinApplicationRequest request)
    {
        return Ok(new
        {
            applicationId = $"MEM-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            status = "Submitted",
            submittedAt = DateTime.UtcNow
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClubDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), clubId = created.Id }, created);
    }

    [HttpPatch("{clubId:int}")]
    public async Task<IActionResult> Update(int clubId, [FromBody] UpdateClubDto dto)
    {
        var updated = await _service.UpdateAsync(clubId, dto);
        return Ok(updated);
    }

    [HttpDelete("{clubId:int}")]
    public async Task<IActionResult> Delete(int clubId)
    {
        await _service.DeleteAsync(clubId);
        return NoContent();
    }

    public sealed class JoinApplicationRequest
    {
        public string LetterOfPurpose { get; set; } = string.Empty;
        public string? PortfolioLinks { get; set; }
        public List<IFormFile>? PortfolioFiles { get; set; }
    }
}
