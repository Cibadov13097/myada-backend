using Asp.Versioning;
using ClubManagementService.API.Security;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/applications")]
[ApiController]
[Authorize]
[Tags("Applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IClubNotificationService _notificationService;
    private readonly ClubManagementDbContext _db;

    public ApplicationsController(IClubNotificationService notificationService, ClubManagementDbContext db)
    {
        _notificationService = notificationService;
        _db = db;
    }

    [HttpGet("{applicationId}/interview-slots")]
    public async Task<IActionResult> GetInterviewSlots(string applicationId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(applicationId, out var appId) || appId <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var app = await _db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken)
            .ConfigureAwait(false);
        if (app == null)
            return NotFound();

        if (!User.IsMyAdaAdministrator() && app.ApplicantId != callerId)
            return Forbid();

        var vacancy = await _db.JobVacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == app.JobVacancyId, cancellationToken)
            .ConfigureAwait(false);
        if (vacancy == null)
            return NotFound(new { message = "Vacancy not found for application." });

        var slotRows = await _db.InterviewSlots
            .AsNoTracking()
            .Where(s => s.ClubId == vacancy.ClubId
                        && s.IsPublished
                        && (s.JobVacancyId == null || s.JobVacancyId == vacancy.Id))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var slotIds = slotRows.Select(s => s.SlotId).ToList();
        var taken = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.InterviewSlotId != null && slotIds.Contains(a.InterviewSlotId))
            .Select(a => new { a.InterviewSlotId, a.Id })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var takenMap = taken
            .Where(x => x.InterviewSlotId != null)
            .ToDictionary(x => x.InterviewSlotId!, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var items = slotRows.Select(s =>
        {
            var isTaken = takenMap.TryGetValue(s.SlotId, out var takenByAppId);
            return new
            {
                id = s.SlotId,
                date = s.StartsAt.ToString("yyyy-MM-dd"),
                startTime = s.StartsAt.ToString("HH:mm"),
                endTime = s.EndsAt.ToString("HH:mm"),
                isTaken,
                takenByMe = isTaken && takenByAppId == appId
            };
        });

        return Ok(new { items });
    }

    [HttpPost("{applicationId}/interview-slot")]
    public async Task<IActionResult> SelectInterviewSlot(string applicationId, [FromBody] SelectInterviewSlotRequest request)
    {
        if (!int.TryParse(applicationId, out var appId) || appId <= 0)
            return NotFound();
        if (request == null || string.IsNullOrWhiteSpace(request.SlotId))
            return BadRequest(new { message = "slotId is required." });
        var slotId = request.SlotId.Trim();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == appId, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (app == null)
            return NotFound(new { message = "Application not found." });

        if (!User.IsMyAdaAdministrator() && app.ApplicantId != callerId)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(app.InterviewSlotId))
        {
            if (string.Equals(app.InterviewSlotId, slotId, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    applicationId = appId.ToString(),
                    status = app.Status.ToString(),
                    interviewSlot = new { id = slotId }
                });
            }

            return Conflict(new { message = "You have already selected an interview slot for this application." });
        }

        var vacancy = await _db.JobVacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == app.JobVacancyId, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (vacancy == null)
            return NotFound(new { message = "Vacancy not found for application." });

        var slot = await _db.InterviewSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SlotId == slotId, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (slot == null)
            return NotFound(new { message = "Interview slot not found." });

        if (!slot.IsPublished || slot.StartsAt.UtcDateTime < DateTime.UtcNow)
            return Conflict(new { message = "This interview slot is not available. Please choose another one." });

        if (slot.ClubId != vacancy.ClubId)
            return Conflict(new { message = "This interview slot does not belong to this application." });
        if (slot.JobVacancyId != null && slot.JobVacancyId != vacancy.Id)
            return Conflict(new { message = "This interview slot does not belong to this application." });

        app.InterviewSlotId = slotId;
        app.Status = JobApplicationStatus.InterviewScheduled;
        app.ModifiedAt = DateTime.UtcNow;
        app.ModifiedByUserId = callerId;

        try
        {
            await _db.SaveChangesAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Unique constraint on JobApplications.InterviewSlotId => slot already taken.
            return Conflict(new { message = "This interview slot has already been taken. Please choose another one." });
        }

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slot Selected",
            Message = $"Application '{applicationId}' selected interview slot '{request.SlotId}'.",
            Source = "club-management",
            Action = "interview_slot.selected",
            EntityType = "application",
            EntityId = applicationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new
        {
            applicationId = appId.ToString(),
            status = "INTERVIEW_SCHEDULED",
            interviewSlot = new
            {
                id = slot.SlotId,
                date = slot.StartsAt.ToString("yyyy-MM-dd"),
                startTime = slot.StartsAt.ToString("HH:mm"),
                endTime = slot.EndsAt.ToString("HH:mm")
            }
        });
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class SelectInterviewSlotRequest
    {
        public string SlotId { get; set; } = string.Empty;
    }
}
