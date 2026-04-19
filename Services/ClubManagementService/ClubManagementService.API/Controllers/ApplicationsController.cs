using Asp.Versioning;
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

    public ApplicationsController(IClubNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("{applicationId}/interview-slots")]
    public IActionResult GetInterviewSlots(string applicationId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("{applicationId}/interview-slot")]
    public async Task<IActionResult> SelectInterviewSlot(string applicationId, [FromBody] SelectInterviewSlotRequest request)
    {
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
            applicationId,
            status = "Interview Scheduled",
            interviewSlotId = request.SlotId
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
