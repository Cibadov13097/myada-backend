using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/club-admin/{clubId:int}")]
[ApiController]
[Authorize]
[Tags("Club Admin")]
public class ClubAdminController : ControllerBase
{
    private readonly IJobVacancyService _vacancyService;
    private readonly IEventService _eventService;
    private readonly IClubNotificationService _notificationService;

    public ClubAdminController(IJobVacancyService vacancyService, IEventService eventService, IClubNotificationService notificationService)
    {
        _vacancyService = vacancyService;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard(int clubId) => Ok(new
    {
        stats = new { activeMembers = 0, openVacancies = 0, newApplications = 0, upcomingEvents = 0 },
        activity = Array.Empty<object>()
    });

    [HttpGet("applications")]
    public IActionResult Applications(int clubId, [FromQuery] string? type = null, [FromQuery] string? status = null, [FromQuery] string? role = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
        => Ok(new { items = Array.Empty<object>(), total = 0, page, limit });

    [HttpGet("applications/{applicationId}")]
    public IActionResult ApplicationById(int clubId, string applicationId) => Ok(new { applicationId });

    [HttpPost("applications/{applicationId}/approve")]
    public async Task<IActionResult> ApproveApplication(int clubId, string applicationId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Membership Application Approved",
            Message = $"Application '{applicationId}' was approved for club '{clubId}'.",
            Source = "club-management",
            Action = "membership_application.approved",
            EntityType = "membership_application",
            EntityId = applicationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { applicationId, status = "approved" });
    }

    [HttpPost("applications/{applicationId}/reject")]
    public async Task<IActionResult> RejectApplication(int clubId, string applicationId, [FromBody] ReasonRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Membership Application Rejected",
            Message = $"Application '{applicationId}' was rejected for club '{clubId}'. Reason: {request.Reason}",
            Source = "club-management",
            Action = "membership_application.rejected",
            EntityType = "membership_application",
            EntityId = applicationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { applicationId, status = "rejected" });
    }

    [HttpPost("applications/{applicationId}/request-changes")]
    public async Task<IActionResult> RequestChanges(int clubId, string applicationId, [FromBody] ChangesRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Membership Application Changes Requested",
            Message = $"Changes were requested for application '{applicationId}'.",
            Source = "club-management",
            Action = "membership_application.changes_requested",
            EntityType = "membership_application",
            EntityId = applicationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { applicationId, status = "changes_requested" });
    }

    [HttpPost("applications/{applicationId}/notes")]
    public async Task<IActionResult> AddNote(int clubId, string applicationId, [FromBody] NoteRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Application Note Added",
            Message = $"A note was added to application '{applicationId}'.",
            Source = "club-management",
            Action = "membership_application.note_added",
            EntityType = "membership_application",
            EntityId = applicationId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { applicationId, note = request.Note });
    }

    [HttpPost("applications/bulk-decision")]
    public async Task<IActionResult> BulkDecision(int clubId, [FromBody] BulkDecisionRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Bulk Application Decision",
            Message = $"Bulk decision '{request.Decision}' was applied to {request.ApplicationIds.Count} applications for club '{clubId}'.",
            Source = "club-management",
            Action = "membership_application.bulk_decision",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { updated = request.ApplicationIds.Count });
    }

    [HttpGet("interview-slots")]
    public IActionResult InterviewSlots(int clubId, [FromQuery] string? vacancy = null) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("interview-slots/generate")]
    public async Task<IActionResult> GenerateInterviewSlots(int clubId, [FromBody] GenerateInterviewSlotsRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slots Generated",
            Message = $"Interview slots were generated for club '{clubId}' ({request.Date} {request.StartTime}-{request.EndTime}).",
            Source = "club-management",
            Action = "interview_slot.generated",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { items = Array.Empty<object>() });
    }

    [HttpPatch("interview-slots/{slotId}")]
    public async Task<IActionResult> PatchInterviewSlot(int clubId, string slotId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slot Updated",
            Message = $"Interview slot '{slotId}' was updated for club '{clubId}'.",
            Source = "club-management",
            Action = "interview_slot.updated",
            EntityType = "interview_slot",
            EntityId = slotId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { slotId });
    }

    [HttpDelete("interview-slots/{slotId}")]
    public async Task<IActionResult> DeleteInterviewSlot(int clubId, string slotId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slot Deleted",
            Message = $"Interview slot '{slotId}' was deleted for club '{clubId}'.",
            Source = "club-management",
            Action = "interview_slot.deleted",
            EntityType = "interview_slot",
            EntityId = slotId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpPost("interview-slots/bulk-assign-vacancy")]
    public async Task<IActionResult> BulkAssignInterviewSlots(int clubId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Interview Slots Assigned",
            Message = $"Interview slots were bulk-assigned to a vacancy for club '{clubId}'.",
            Source = "club-management",
            Action = "interview_slot.bulk_assign",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpGet("vacancies")]
    public async Task<IActionResult> Vacancies(int clubId) => Ok(await _vacancyService.GetByClubIdAsync(clubId));

    [HttpPost("vacancies")]
    public async Task<IActionResult> CreateVacancy(int clubId, [FromBody] CreateVacancyRequest request)
    {
        var created = await _vacancyService.CreateAsync(new CreateJobVacancyDto
        {
            ClubId = clubId,
            Title = request.Title,
            Description = request.Description,
            PostedBy = GetCurrentUserIdOrDefault()
        });

        return Ok(created);
    }

    [HttpPatch("vacancies/{vacancyId:int}")]
    public async Task<IActionResult> PatchVacancy(int clubId, int vacancyId, [FromBody] UpdateVacancyRequest request)
    {
        var updated = await _vacancyService.UpdateAsync(vacancyId, new UpdateJobVacancyDto { Title = request.Title, Description = request.Description });
        return Ok(updated);
    }

    [HttpPatch("vacancies/{vacancyId:int}/status")]
    public async Task<IActionResult> PatchVacancyStatus(int clubId, int vacancyId, [FromBody] StatusRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Status Updated",
            Message = $"Vacancy '{vacancyId}' status changed to '{request.Status}'.",
            Source = "club-management",
            Action = "vacancy.status_updated",
            EntityType = "vacancy",
            EntityId = vacancyId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { vacancyId, status = request.Status });
    }

    [HttpGet("positions")]
    public IActionResult Positions(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition(int clubId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Position Created",
            Message = $"A new position was created for club '{clubId}'.",
            Source = "club-management",
            Action = "position.created",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpPatch("positions/{positionId}")]
    public async Task<IActionResult> PatchPosition(int clubId, string positionId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Position Updated",
            Message = $"Position '{positionId}' was updated for club '{clubId}'.",
            Source = "club-management",
            Action = "position.updated",
            EntityType = "position",
            EntityId = positionId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpDelete("positions/{positionId}")]
    public async Task<IActionResult> DeletePosition(int clubId, string positionId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Position Deleted",
            Message = $"Position '{positionId}' was deleted for club '{clubId}'.",
            Source = "club-management",
            Action = "position.deleted",
            EntityType = "position",
            EntityId = positionId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpGet("members")]
    public IActionResult Members(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpDelete("members/{memberId}")]
    public async Task<IActionResult> DeleteMember(int clubId, string memberId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Member Removed",
            Message = $"Member '{memberId}' was removed from club '{clubId}'.",
            Source = "club-management",
            Action = "member.deleted",
            EntityType = "member",
            EntityId = memberId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpGet("employees")]
    public IActionResult Employees(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(int clubId, [FromBody] CreateEmployeeRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Employee Added",
            Message = $"Employee '{request.StudentId}' was added to club '{clubId}' as '{request.Position}'.",
            Source = "club-management",
            Action = "employee.created",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpPatch("employees/positions")]
    public async Task<IActionResult> PatchEmployeePositions(int clubId, [FromBody] UpdateEmployeePositionsRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Employee Positions Updated",
            Message = $"{request.Changes.Count} employee position changes were applied for club '{clubId}'.",
            Source = "club-management",
            Action = "employee.positions_updated",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpDelete("employees/{employeeId}")]
    public async Task<IActionResult> DeleteEmployee(int clubId, string employeeId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Employee Removed",
            Message = $"Employee '{employeeId}' was removed from club '{clubId}'.",
            Source = "club-management",
            Action = "employee.deleted",
            EntityType = "employee",
            EntityId = employeeId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(int clubId, [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var events = await _eventService.GetAllAsync();
        return Ok(new { items = events });
    }

    [HttpPatch("events/{eventId:int}")]
    public async Task<IActionResult> PatchEvent(int clubId, int eventId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Patched",
            Message = $"Event '{eventId}' was patched for club '{clubId}'.",
            Source = "club-management",
            Action = "event.patched",
            EntityType = "event",
            EntityId = eventId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { eventId });
    }

    [HttpPost("events/proposals")]
    public async Task<IActionResult> EventProposal(int clubId, [FromBody] object body)
    {
        var proposalId = $"EVP-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Submitted",
            Message = $"Event proposal '{proposalId}' was submitted for club '{clubId}'.",
            Source = "club-management",
            Action = "event_proposal.created",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId });
    }

    [HttpGet("events/{eventId:int}/attendees")]
    public IActionResult EventAttendees(int clubId, int eventId) => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("events/{eventId:int}/attendees/{attendeeId}")]
    public async Task<IActionResult> PatchAttendee(int clubId, int eventId, string attendeeId, [FromBody] AttendeePatchRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Attendee Updated",
            Message = $"Attendee '{attendeeId}' was updated for event '{eventId}' (entered: {request.Entered}).",
            Source = "club-management",
            Action = "event_attendee.updated",
            EntityType = "event_attendee",
            EntityId = attendeeId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { attendeeId, request.Entered });
    }

    [HttpDelete("events/{eventId:int}/attendees/{attendeeId}")]
    public async Task<IActionResult> DeleteAttendee(int clubId, int eventId, string attendeeId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Attendee Removed",
            Message = $"Attendee '{attendeeId}' was removed from event '{eventId}'.",
            Source = "club-management",
            Action = "event_attendee.deleted",
            EntityType = "event_attendee",
            EntityId = attendeeId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpPatch("profile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PatchProfile(int clubId, [FromForm] ClubProfilePatchRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Profile Updated",
            Message = $"Club '{clubId}' profile was updated.",
            Source = "club-management",
            Action = "club.profile_updated",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { clubId, updated = true });
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> AddAnnouncement(int clubId, [FromBody] AnnouncementRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Announcement",
            Message = $"Announcement posted for club '{clubId}': {request.Title}.",
            Source = "club-management",
            Action = "club.announcement_created",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { request.Title, request.Message });
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class ReasonRequest { public string Reason { get; set; } = string.Empty; }
    public sealed class ChangesRequest { public string Changes { get; set; } = string.Empty; }
    public sealed class NoteRequest { public string Note { get; set; } = string.Empty; }
    public sealed class StatusRequest { public string Status { get; set; } = string.Empty; }
    public sealed class AttendeePatchRequest { public bool Entered { get; set; } }

    public sealed class BulkDecisionRequest
    {
        public List<int> ApplicationIds { get; set; } = new();
        public string Decision { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class GenerateInterviewSlotsRequest
    {
        public string Date { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public int InterviewLength { get; set; }
        public string Vacancy { get; set; } = string.Empty;
    }

    public sealed class CreateVacancyRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class UpdateVacancyRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class CreateEmployeeRequest
    {
        public string StudentId { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }

    public sealed class UpdateEmployeePositionsRequest
    {
        public List<UpdateEmployeePositionItem> Changes { get; set; } = new();
    }

    public sealed class UpdateEmployeePositionItem
    {
        public int Id { get; set; }
        public string Position { get; set; } = string.Empty;
    }

    public sealed class ClubProfilePatchRequest
    {
        public string? SocialLinks { get; set; }
        public string? FocusAreasJson { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? BackgroundFile { get; set; }
    }

    public sealed class AnnouncementRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
