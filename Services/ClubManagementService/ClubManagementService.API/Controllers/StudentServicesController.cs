using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/student-services")]
[ApiController]
[Authorize]
[Tags("Student Services")]
public class StudentServicesController : ControllerBase
{
    private readonly IClubService _clubService;
    private readonly IEventService _eventService;
    private readonly IClubNotificationService _notificationService;

    public StudentServicesController(IClubService clubService, IEventService eventService, IClubNotificationService notificationService)
    {
        _clubService = clubService;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard() => Ok(new { cards = Array.Empty<object>() });

    [HttpGet("clubs")]
    public async Task<IActionResult> Clubs([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var clubs = await _clubService.GetAllAsync();
        return Ok(new { items = clubs, total = clubs.Count, page, limit });
    }

    [HttpGet("clubs/{clubId:int}")]
    public async Task<IActionResult> ClubById(int clubId)
    {
        var club = await _clubService.GetByIdAsync(clubId);
        return club == null ? NotFound() : Ok(club);
    }

    [HttpGet("club-proposals")]
    public IActionResult ClubProposals([FromQuery] string? status = null, [FromQuery] string? search = null) => Ok(new { items = Array.Empty<object>() });

    [HttpGet("club-proposals/{proposalId}")]
    public IActionResult ClubProposalById(string proposalId) => Ok(new { proposalId });

    [HttpPost("club-proposals/{proposalId}/approve")]
    public async Task<IActionResult> ApproveClubProposal(string proposalId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Approved",
            Message = $"Club proposal '{proposalId}' was approved.",
            Source = "club-management",
            Action = "club_proposal.approved",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "approved" });
    }

    [HttpPost("club-proposals/{proposalId}/reject")]
    public async Task<IActionResult> RejectClubProposal(string proposalId, [FromBody] ReasonRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Rejected",
            Message = $"Club proposal '{proposalId}' was rejected. Reason: {request.Reason}",
            Source = "club-management",
            Action = "club_proposal.rejected",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "rejected" });
    }

    [HttpPost("club-proposals/{proposalId}/request-revision")]
    public async Task<IActionResult> RequestClubProposalRevision(string proposalId, [FromBody] ChangesRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Revision Requested",
            Message = $"Revision requested for club proposal '{proposalId}'.",
            Source = "club-management",
            Action = "club_proposal.revision_requested",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "under_revision" });
    }

    [HttpGet("club-proposal-requirements")]
    public IActionResult ClubProposalRequirements() => Ok(new { requirements = Array.Empty<string>(), deadline = (DateTime?)null });

    [HttpPut("club-proposal-requirements")]
    public async Task<IActionResult> UpdateClubProposalRequirements([FromBody] ClubProposalRequirementsRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Proposal Requirements Updated",
            Message = $"Club proposal requirements were updated ({request.Requirements.Count} requirement items).",
            Source = "club-management",
            Action = "club_proposal_requirements.updated",
            EntityType = "club_proposal_requirements",
            EntityId = "active",
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(request);
    }

    [HttpGet("event-proposals")]
    public IActionResult EventProposals([FromQuery] string? search = null, [FromQuery] string? status = null) => Ok(new { items = Array.Empty<object>() });

    [HttpGet("event-proposals/{proposalId}")]
    public IActionResult EventProposalById(string proposalId) => Ok(new { proposalId });

    [HttpPost("event-proposals/{proposalId}/approve")]
    public async Task<IActionResult> ApproveEventProposal(string proposalId, [FromBody] ApproveEventProposalRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Approved",
            Message = $"Event proposal '{proposalId}' was approved.",
            Source = "club-management",
            Action = "event_proposal.approved",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "approved" });
    }

    [HttpPost("event-proposals/{proposalId}/reject")]
    public async Task<IActionResult> RejectEventProposal(string proposalId, [FromBody] ReasonRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Rejected",
            Message = $"Event proposal '{proposalId}' was rejected. Reason: {request.Reason}",
            Source = "club-management",
            Action = "event_proposal.rejected",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "rejected" });
    }

    [HttpPost("event-proposals/{proposalId}/request-revision")]
    public async Task<IActionResult> RequestEventProposalRevision(string proposalId, [FromBody] ChangesRequest request)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Revision Requested",
            Message = $"Revision requested for event proposal '{proposalId}'.",
            Source = "club-management",
            Action = "event_proposal.revision_requested",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { proposalId, status = "under_revision" });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var events = await _eventService.GetAllAsync();
        return Ok(new { items = events });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var created = await _eventService.CreateAsync(new CreateEventDto
        {
            Name = request.Name,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location,
            SeatLimit = request.SeatLimit,
            Requirements = request.Requirements,
            Prerequisites = request.Prerequisites,
            Notes = request.Notes,
            ApprovedBy = GetCurrentUserIdOrDefault()
        });

        return Ok(created);
    }

    [HttpPatch("events/{eventId:int}")]
    public async Task<IActionResult> PatchEvent(int eventId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Patched",
            Message = $"Event '{eventId}' was patched by student services.",
            Source = "club-management",
            Action = "event.patched",
            EntityType = "event",
            EntityId = eventId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { eventId });
    }

    [HttpGet("events/{eventId:int}")]
    public async Task<IActionResult> EventById(int eventId)
    {
        var evt = await _eventService.GetByIdAsync(eventId);
        return evt == null ? NotFound() : Ok(evt);
    }

    [HttpDelete("events/{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        await _eventService.DeleteAsync(eventId);
        return NoContent();
    }

    [HttpPatch("clubs/{clubId:int}")]
    public async Task<IActionResult> PatchClub(int clubId, [FromBody] UpdateClubRequest request)
    {
        var updated = await _clubService.UpdateAsync(clubId, new UpdateClubDto
        {
            Name = request.Name,
            Description = request.Description,
            Status = ParseClubStatus(request.Status)
        });

        return Ok(updated);
    }

    [HttpPost("clubs/{clubId:int}/profile-image/approve")]
    public async Task<IActionResult> ApproveClubProfileImage(int clubId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Profile Image Approved",
            Message = $"Profile image for club '{clubId}' was approved.",
            Source = "club-management",
            Action = "club.profile_image_approved",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { clubId, approved = true });
    }

    [HttpGet("clubs/{clubId:int}/members")]
    public IActionResult ClubMembers(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("clubs/{clubId:int}/members/{memberId}")]
    public async Task<IActionResult> PatchClubMember(int clubId, string memberId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Member Updated",
            Message = $"Member '{memberId}' was updated in club '{clubId}'.",
            Source = "club-management",
            Action = "member.updated",
            EntityType = "member",
            EntityId = memberId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { memberId });
    }

    [HttpDelete("clubs/{clubId:int}/members/{memberId}")]
    public async Task<IActionResult> DeleteClubMember(int clubId, string memberId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Member Removed",
            Message = $"Member '{memberId}' was removed from club '{clubId}'.",
            Source = "club-management",
            Action = "member.deleted",
            EntityType = "member",
            EntityId = memberId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    [HttpGet("clubs/{clubId:int}/employees")]
    public IActionResult ClubEmployees(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("clubs/{clubId:int}/employees")]
    public async Task<IActionResult> CreateClubEmployee(int clubId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Employee Added",
            Message = $"A new employee was added to club '{clubId}'.",
            Source = "club-management",
            Action = "employee.created",
            EntityType = "club",
            EntityId = clubId.ToString(),
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok();
    }

    [HttpPatch("clubs/{clubId:int}/employees/{employeeId}")]
    public async Task<IActionResult> PatchClubEmployee(int clubId, string employeeId, [FromBody] object body)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Employee Updated",
            Message = $"Employee '{employeeId}' was updated for club '{clubId}'.",
            Source = "club-management",
            Action = "employee.updated",
            EntityType = "employee",
            EntityId = employeeId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new { employeeId });
    }

    [HttpDelete("clubs/{clubId:int}/employees/{employeeId}")]
    public async Task<IActionResult> DeleteClubEmployee(int clubId, string employeeId)
    {
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Employee Removed",
            Message = $"Employee '{employeeId}' was removed from club '{clubId}'.",
            Source = "club-management",
            Action = "employee.deleted",
            EntityType = "employee",
            EntityId = employeeId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return NoContent();
    }

    private static ClubStatus ParseClubStatus(string? value)
    {
        return Enum.TryParse<ClubStatus>(value, true, out var status) ? status : ClubStatus.Pending;
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class ReasonRequest { public string Reason { get; set; } = string.Empty; }
    public sealed class ChangesRequest { public string Changes { get; set; } = string.Empty; }

    public sealed class ClubProposalRequirementsRequest
    {
        public List<string> Requirements { get; set; } = new();
        public DateTime? Deadline { get; set; }
    }

    public sealed class ApproveEventProposalRequest
    {
        public List<SubEventAssignmentRequest> Assignments { get; set; } = new();
    }

    public sealed class SubEventAssignmentRequest
    {
        public int SubEventIndex { get; set; }
        public string BuildingId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
    }

    public sealed class CreateEventRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);
        public string Location { get; set; } = string.Empty;
        public int SeatLimit { get; set; }
        public string Requirements { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public sealed class UpdateClubRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }
}
