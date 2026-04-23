using Asp.Versioning;
using ClubManagementService.API.Security;
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
    private readonly IInterviewSchedulingService _interviewScheduling;

    public ApplicationsController(
        IClubNotificationService notificationService,
        IInterviewSchedulingService interviewScheduling)
    {
        _notificationService = notificationService;
        _interviewScheduling = interviewScheduling;
    }

    /// <summary>Same as <c>GET /api/v1/interviews/{applicationId}/slots</c> — list bookable slots for this application.</summary>
    [HttpGet("{applicationId}/interview-slots")]
    public async Task<IActionResult> GetInterviewSlots(
        string applicationId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] DateOnly? date = null,
        [FromQuery] bool includeFull = false,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(applicationId, out var id) || id <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var result = await _interviewScheduling
            .ListSlotsAsync(id, callerId, User.IsMyAdaAdministrator(), from, to, date, includeFull, cancellationToken)
            .ConfigureAwait(false);

        if (result.NotFound)
            return NotFound(new { message = result.Message });
        if (result.Forbidden)
            return Forbid();

        return Ok(new { items = result.Slots });
    }

    /// <summary>Book a slot (same behavior as <c>POST /api/v1/interviews/{applicationId}/bookings</c>).</summary>
    [HttpPost("{applicationId}/interview-slot")]
    public async Task<IActionResult> SelectInterviewSlot(string applicationId, [FromBody] SelectInterviewSlotRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(applicationId, out var id) || id <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var r = await _interviewScheduling
            .BookSlotAsync(id, callerId, User.IsMyAdaAdministrator(), request?.SlotId ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (r.Forbidden)
            return Forbid();
        if (r.NotFound)
            return NotFound(new { message = r.Message });
        if (r.BadRequest)
            return BadRequest(new { message = r.Message });
        if (r.Conflict)
            return Conflict(new { message = r.Message });

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slot Selected",
            Message = $"Application '{applicationId}' selected interview slot '{r.SlotId}'.",
            Source = "club-management",
            Action = "interview_slot.selected",
            EntityType = "application",
            EntityId = applicationId,
            ActorUserId = callerId
        });

        return Ok(new
        {
            applicationId,
            status = "Interview Scheduled",
            interviewSlotId = r.SlotId,
            startAt = r.StartAt,
            endAt = r.EndAt
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
