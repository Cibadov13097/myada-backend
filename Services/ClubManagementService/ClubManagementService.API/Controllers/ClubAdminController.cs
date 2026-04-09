using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/club-admin/{clubId:int}")]
[ApiController]
[Authorize]
[Tags("Club Admin")]
public class ClubAdminController : ControllerBase
{
    private readonly IJobVacancyService _vacancyService;
    private readonly IEventService _eventService;

    public ClubAdminController(IJobVacancyService vacancyService, IEventService eventService)
    {
        _vacancyService = vacancyService;
        _eventService = eventService;
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
    public IActionResult ApproveApplication(int clubId, string applicationId) => Ok(new { applicationId, status = "approved" });

    [HttpPost("applications/{applicationId}/reject")]
    public IActionResult RejectApplication(int clubId, string applicationId, [FromBody] ReasonRequest request) => Ok(new { applicationId, status = "rejected" });

    [HttpPost("applications/{applicationId}/request-changes")]
    public IActionResult RequestChanges(int clubId, string applicationId, [FromBody] ChangesRequest request) => Ok(new { applicationId, status = "changes_requested" });

    [HttpPost("applications/{applicationId}/notes")]
    public IActionResult AddNote(int clubId, string applicationId, [FromBody] NoteRequest request) => Ok(new { applicationId, note = request.Note });

    [HttpPost("applications/bulk-decision")]
    public IActionResult BulkDecision(int clubId, [FromBody] BulkDecisionRequest request) => Ok(new { updated = request.ApplicationIds.Count });

    [HttpGet("interview-slots")]
    public IActionResult InterviewSlots(int clubId, [FromQuery] string? vacancy = null) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("interview-slots/generate")]
    public IActionResult GenerateInterviewSlots(int clubId, [FromBody] GenerateInterviewSlotsRequest request) => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("interview-slots/{slotId}")]
    public IActionResult PatchInterviewSlot(int clubId, string slotId, [FromBody] object body) => Ok(new { slotId });

    [HttpDelete("interview-slots/{slotId}")]
    public IActionResult DeleteInterviewSlot(int clubId, string slotId) => NoContent();

    [HttpPost("interview-slots/bulk-assign-vacancy")]
    public IActionResult BulkAssignInterviewSlots(int clubId, [FromBody] object body) => Ok();

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
    public IActionResult PatchVacancyStatus(int clubId, int vacancyId, [FromBody] StatusRequest request) => Ok(new { vacancyId, status = request.Status });

    [HttpGet("positions")]
    public IActionResult Positions(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("positions")]
    public IActionResult CreatePosition(int clubId, [FromBody] object body) => Ok();

    [HttpPatch("positions/{positionId}")]
    public IActionResult PatchPosition(int clubId, string positionId, [FromBody] object body) => Ok();

    [HttpDelete("positions/{positionId}")]
    public IActionResult DeletePosition(int clubId, string positionId) => NoContent();

    [HttpGet("members")]
    public IActionResult Members(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpDelete("members/{memberId}")]
    public IActionResult DeleteMember(int clubId, string memberId) => NoContent();

    [HttpGet("employees")]
    public IActionResult Employees(int clubId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("employees")]
    public IActionResult CreateEmployee(int clubId, [FromBody] CreateEmployeeRequest request) => Ok();

    [HttpPatch("employees/positions")]
    public IActionResult PatchEmployeePositions(int clubId, [FromBody] UpdateEmployeePositionsRequest request) => Ok();

    [HttpDelete("employees/{employeeId}")]
    public IActionResult DeleteEmployee(int clubId, string employeeId) => NoContent();

    [HttpGet("events")]
    public async Task<IActionResult> Events(int clubId, [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var events = await _eventService.GetAllAsync();
        return Ok(new { items = events });
    }

    [HttpPatch("events/{eventId:int}")]
    public IActionResult PatchEvent(int clubId, int eventId, [FromBody] object body) => Ok(new { eventId });

    [HttpPost("events/proposals")]
    public IActionResult EventProposal(int clubId, [FromBody] object body) => Ok(new { proposalId = $"EVP-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" });

    [HttpGet("events/{eventId:int}/attendees")]
    public IActionResult EventAttendees(int clubId, int eventId) => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("events/{eventId:int}/attendees/{attendeeId}")]
    public IActionResult PatchAttendee(int clubId, int eventId, string attendeeId, [FromBody] AttendeePatchRequest request) => Ok(new { attendeeId, request.Entered });

    [HttpDelete("events/{eventId:int}/attendees/{attendeeId}")]
    public IActionResult DeleteAttendee(int clubId, int eventId, string attendeeId) => NoContent();

    [HttpPatch("profile")]
    [Consumes("multipart/form-data")]
    public IActionResult PatchProfile(int clubId, [FromForm] ClubProfilePatchRequest request) => Ok(new { clubId, updated = true });

    [HttpPost("announcements")]
    public IActionResult AddAnnouncement(int clubId, [FromBody] AnnouncementRequest request) => Ok(new { request.Title, request.Message });

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
