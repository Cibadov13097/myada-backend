using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-position-requirements")]
[ApiController]
[Authorize]
[Tags("Club Position Requirements")]
public class ClubPositionRequirementsController : ControllerBase
{
    private readonly ClubManagementDbContext _db;

    public ClubPositionRequirementsController(ClubManagementDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clubPositionId = null)
    {
        var query = _db.ClubPositionRequirements.AsNoTracking().AsQueryable();
        if (clubPositionId is > 0)
            query = query.Where(r => r.ClubPositionId == clubPositionId.Value);

        var list = await query
            .OrderBy(r => r.ClubPositionId)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.ClubPositionId,
                r.SortOrder,
                r.Text
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _db.ClubPositionRequirements
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.ClubPositionId,
                r.SortOrder,
                r.Text
            })
            .FirstOrDefaultAsync(HttpContext.RequestAborted);

        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClubPositionRequirementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Text is required." });

        var positionExists = await _db.ClubPositions
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ClubPositionId, HttpContext.RequestAborted);
        if (!positionExists)
            return NotFound(new { message = $"ClubPosition {request.ClubPositionId} not found." });

        var entity = new Domain.Entities.ClubPositionRequirement
        {
            ClubPositionId = request.ClubPositionId,
            SortOrder = request.SortOrder,
            Text = request.Text.Trim()
        };

        await _db.ClubPositionRequirements.AddAsync(entity, HttpContext.RequestAborted);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), id = entity.Id }, new
        {
            entity.Id,
            entity.ClubPositionId,
            entity.SortOrder,
            entity.Text
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClubPositionRequirementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Text is required." });

        var entity = await _db.ClubPositionRequirements.FirstOrDefaultAsync(r => r.Id == id, HttpContext.RequestAborted);
        if (entity == null)
            return NotFound();

        var positionExists = await _db.ClubPositions
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ClubPositionId, HttpContext.RequestAborted);
        if (!positionExists)
            return NotFound(new { message = $"ClubPosition {request.ClubPositionId} not found." });

        entity.ClubPositionId = request.ClubPositionId;
        entity.SortOrder = request.SortOrder;
        entity.Text = request.Text.Trim();

        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(new
        {
            entity.Id,
            entity.ClubPositionId,
            entity.SortOrder,
            entity.Text
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.ClubPositionRequirements.FirstOrDefaultAsync(r => r.Id == id, HttpContext.RequestAborted);
        if (entity == null)
            return NotFound();

        _db.ClubPositionRequirements.Remove(entity);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    public sealed class CreateClubPositionRequirementRequest
    {
        public int ClubPositionId { get; set; }
        public int SortOrder { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public sealed class UpdateClubPositionRequirementRequest
    {
        public int ClubPositionId { get; set; }
        public int SortOrder { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
