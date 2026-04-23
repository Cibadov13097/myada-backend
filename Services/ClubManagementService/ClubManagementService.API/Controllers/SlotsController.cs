using Asp.Versioning;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/slots")]
[ApiController]
[Authorize]
[Tags("Slots")]
public sealed class SlotsController : ControllerBase
{
    private readonly ClubManagementDbContext _db;

    public SlotsController(ClubManagementDbContext db)
    {
        _db = db;
    }

    [HttpGet("{slotId}")]
    public async Task<IActionResult> GetSlot(string slotId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return BadRequest(new { message = "slotId is required." });

        var id = slotId.Trim();

        var slot = await _db.InterviewSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SlotId == id, cancellationToken)
            .ConfigureAwait(false);
        if (slot == null)
            return NotFound();

        var bookedCount = await _db.JobApplications
            .AsNoTracking()
            .CountAsync(a => a.InterviewSlotId != null && a.InterviewSlotId == id, cancellationToken)
            .ConfigureAwait(false);

        var remaining = Math.Max(0, slot.Capacity - bookedCount);

        return Ok(new
        {
            id = slot.SlotId,
            startAt = slot.StartsAt,
            endAt = slot.EndsAt,
            timezone = "UTC",
            capacity = slot.Capacity,
            bookedCount,
            remainingCapacity = remaining,
            status = slot.IsPublished ? "ACTIVE" : "CANCELLED",
            bookable = slot.IsPublished && slot.StartsAt > DateTimeOffset.UtcNow && remaining > 0,
            locationType = (string?)null,
            meetingUrl = (string?)null,
            interviewerName = (string?)null
        });
    }
}

