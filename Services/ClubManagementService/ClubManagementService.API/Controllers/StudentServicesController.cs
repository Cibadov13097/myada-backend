using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/student-services")]
[ApiController]
[Authorize]
[Tags("Student Services")]
public class StudentServicesController : ControllerBase
{
    private readonly IClubService _clubService;
    private readonly IEventService _eventService;

    public StudentServicesController(IClubService clubService, IEventService eventService)
    {
        _clubService = clubService;
        _eventService = eventService;
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
    public IActionResult ApproveClubProposal(string proposalId) => Ok(new { proposalId, status = "approved" });

    [HttpPost("club-proposals/{proposalId}/reject")]
    public IActionResult RejectClubProposal(string proposalId, [FromBody] ReasonRequest request) => Ok(new { proposalId, status = "rejected" });

    [HttpPost("club-proposals/{proposalId}/request-revision")]
    public IActionResult RequestClubProposalRevision(string proposalId, [FromBody] ChangesRequest request) => Ok(new { proposalId, status = "under_revision" });

    [HttpGet("club-proposal-requirements")]
    public IActionResult ClubProposalRequirements() => Ok(new { requirements = Array.Empty<string>(), deadline = (DateTime?)null });

    [HttpPut("club-proposal-requirements")]
    public IActionResult UpdateClubProposalRequirements([FromBody] ClubProposalRequirementsRequest request) => Ok(request);

    [HttpGet("event-proposals")]
    public IActionResult EventProposals([FromQuery] string? search = null, [FromQuery] string? status = null) => Ok(new { items = Array.Empty<object>() });

    [HttpGet("event-proposals/{proposalId}")]
    public IActionResult EventProposalById(string proposalId) => Ok(new { proposalId });

    [HttpPost("event-proposals/{proposalId}/approve")]
    public IActionResult ApproveEventProposal(string proposalId, [FromBody] ApproveEventProposalRequest request) => Ok(new { proposalId, status = "approved" });

    [HttpPost("event-proposals/{proposalId}/reject")]
    public IActionResult RejectEventProposal(string proposalId, [FromBody] ReasonRequest request) => Ok(new { proposalId, status = "rejected" });

    [HttpPost("event-proposals/{proposalId}/request-revision")]
    public IActionResult RequestEventProposalRevision(string proposalId, [FromBody] ChangesRequest request) => Ok(new { proposalId, status = "under_revision" });

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
    public IActionResult PatchEvent(int eventId, [FromBody] object body) => Ok(new { eventId });

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
    public IActionResult ApproveClubProfileImage(int clubId) => Ok(new { clubId, approved = true });

    [HttpGet("clubs/{clubId:int}/members")]
    public IActionResult ClubMembers(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("clubs/{clubId:int}/members/{memberId}")]
    public IActionResult PatchClubMember(int clubId, string memberId, [FromBody] object body) => Ok(new { memberId });

    [HttpDelete("clubs/{clubId:int}/members/{memberId}")]
    public IActionResult DeleteClubMember(int clubId, string memberId) => NoContent();

    [HttpGet("clubs/{clubId:int}/employees")]
    public IActionResult ClubEmployees(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("clubs/{clubId:int}/employees")]
    public IActionResult CreateClubEmployee(int clubId, [FromBody] object body) => Ok();

    [HttpPatch("clubs/{clubId:int}/employees/{employeeId}")]
    public IActionResult PatchClubEmployee(int clubId, string employeeId, [FromBody] object body) => Ok(new { employeeId });

    [HttpDelete("clubs/{clubId:int}/employees/{employeeId}")]
    public IActionResult DeleteClubEmployee(int clubId, string employeeId) => NoContent();

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
