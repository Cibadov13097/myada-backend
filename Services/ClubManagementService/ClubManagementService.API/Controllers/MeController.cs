using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/me")]
[ApiController]
[Authorize]
[Tags("Me")]
public class MeController : ControllerBase
{
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IEventRegistrationService _eventRegistrationService;
    private readonly IClubNotificationService _notificationService;

    public MeController(
        IJobApplicationService jobApplicationService,
        IEventRegistrationService eventRegistrationService,
        IClubNotificationService notificationService)
    {
        _jobApplicationService = jobApplicationService;
        _eventRegistrationService = eventRegistrationService;
        _notificationService = notificationService;
    }

    [HttpGet("club-memberships")]
    public IActionResult GetClubMemberships() => Ok(new { items = Array.Empty<object>() });

    //[HttpGet("saved-vacancies")]
    //public IActionResult GetSavedVacancies() => Ok(new { items = Array.Empty<object>() });

    //[HttpPost("saved-vacancies/{vacancyId:int}")]
    //public IActionResult SaveVacancy(int vacancyId) => Ok(new { vacancyId, saved = true });

    //[HttpDelete("saved-vacancies/{vacancyId:int}")]
    //public IActionResult RemoveSavedVacancy(int vacancyId) => NoContent();

    [HttpGet("vacancy-applications")]
    public async Task<IActionResult> GetVacancyApplications()
    {
        var mine = (await _jobApplicationService.GetAllAsync()).Where(a => a.ApplicantId == GetCurrentUserIdOrDefault());
        return Ok(new { items = mine });
    }

    [HttpGet("membership-applications")]
    public IActionResult GetMembershipApplications() => Ok(new { items = Array.Empty<object>() });

    [HttpGet("event-registrations")]
    public async Task<IActionResult> GetEventRegistrations()
    {
        var mine = (await _eventRegistrationService.GetAllAsync()).Where(a => a.UserId == GetCurrentUserIdOrDefault());
        return Ok(new { items = mine });
    }

    [HttpGet("club-notifications")]
    public IActionResult GetClubNotifications([FromQuery] string type = "all") => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("club-notifications/{notificationId}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(string notificationId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Notification Read",
            Message = $"Notification '{notificationId}' was marked as read.",
            Source = "club-management",
            Action = "notification.read",
            EntityType = "club_notification",
            EntityId = notificationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { notificationId, read = true });
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }
}
