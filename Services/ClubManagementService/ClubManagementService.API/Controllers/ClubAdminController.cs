using Asp.Versioning;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-admin/{clubId}")]
[ApiController]
[Authorize]
[Tags("Club Admin")]
public class ClubAdminController : ControllerBase
{
    private readonly IClubAdminService _clubAdmin;

    public ClubAdminController(IClubAdminService clubAdmin) => _clubAdmin = clubAdmin;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(string clubId, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.GetDashboardAsync(clubId, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet("applications")]
    public async Task<IActionResult> Applications(string clubId, [FromQuery] string? type = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetApplicationsAsync(clubId, type, status, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpGet("applications/{applicationId}")]
    public async Task<IActionResult> ApplicationById(string clubId, string applicationId, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.GetApplicationAsync(clubId, applicationId, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("applications/{applicationId}/approve")]
    public async Task<IActionResult> ApproveApplication(string clubId, string applicationId, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.ApproveApplicationAsync(clubId, applicationId, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("applications/{applicationId}/reject")]
    public async Task<IActionResult> RejectApplication(string clubId, string applicationId, [FromBody] ReasonRequest request, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.RejectApplicationAsync(clubId, applicationId, request.Reason, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("applications/{applicationId}/request-changes")]
    public async Task<IActionResult> RequestChanges(string clubId, string applicationId, [FromBody] ChangesRequest request, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.RequestChangesAsync(clubId, applicationId, request.Changes, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("applications/{applicationId}/notes")]
    public async Task<IActionResult> AddNote(string clubId, string applicationId, [FromBody] NoteRequest request, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.AddApplicationNoteAsync(clubId, applicationId, request.Note, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("applications/bulk-decision")]
    public async Task<IActionResult> BulkDecision(string clubId, [FromBody] BulkDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _clubAdmin.BulkDecisionAsync(clubId, request.ApplicationIds, request.Decision, request.Reason, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("interview-slots")]
    public async Task<IActionResult> InterviewSlots(string clubId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetInterviewSlotsAsync(clubId, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPost("interview-slots/generate")]
    public async Task<IActionResult> GenerateInterviewSlots(string clubId, [FromBody] GenerateInterviewSlotsRequest request, CancellationToken cancellationToken)
    {
        var result = await _clubAdmin.GenerateInterviewSlotsAsync(clubId, new GenerateInterviewSlotsInput
        {
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            InterviewLengthMinutes = request.InterviewLength,
            VacancyId = string.IsNullOrWhiteSpace(request.Vacancy) ? null : request.Vacancy
        }, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPatch("interview-slots/{slotId}")]
    public async Task<IActionResult> PatchInterviewSlot(string clubId, string slotId, [FromBody] PatchInterviewSlotBody body, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.PatchInterviewSlotAsync(clubId, slotId, new PatchInterviewSlotInput
        {
            StartsAt = body.StartsAt,
            EndsAt = body.EndsAt,
            VacancyId = body.VacancyId,
            Capacity = body.Capacity
        }, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpDelete("interview-slots/{slotId}")]
    public async Task<IActionResult> DeleteInterviewSlot(string clubId, string slotId, CancellationToken cancellationToken)
    {
        var ok = await _clubAdmin.DeleteInterviewSlotAsync(clubId, slotId, cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("vacancies")]
    public async Task<IActionResult> Vacancies(string clubId, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetVacanciesAsync(clubId, status, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPost("vacancies")]
    public async Task<IActionResult> CreateVacancy(string clubId, [FromBody] CreateVacancyRequest request, CancellationToken cancellationToken)
    {
        var title = request.Title ?? string.Empty;
        var created = await _clubAdmin.CreateVacancyAsync(clubId, new CreateJobVacancyDto
        {
            ClubId = 0,
            Title = title,
            Description = request.Description,
            PostedBy = GetCurrentUserIdOrDefault(),
            ClubPositionId = request.PositionId,
            ApplicationDeadline = request.ApplicationDeadline,
            Publish = request.Publish
        }, cancellationToken).ConfigureAwait(false);
        return Ok(created);
    }

    [HttpPatch("vacancies/{vacancyId}")]
    public async Task<IActionResult> PatchVacancy(string clubId, string vacancyId, [FromBody] UpdateVacancyRequest request, CancellationToken cancellationToken)
    {
        var updated = await _clubAdmin.PatchVacancyAsync(clubId, vacancyId, new UpdateJobVacancyDto
        {
            Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive,
            ClubPositionId = request.PositionId,
            ApplicationDeadline = request.ApplicationDeadline,
            IsDraft = request.IsDraft
        }, cancellationToken).ConfigureAwait(false);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpPatch("vacancies/{vacancyId}/status")]
    public async Task<IActionResult> PatchVacancyStatus(string clubId, string vacancyId, [FromBody] StatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await _clubAdmin.PatchVacancyStatusAsync(clubId, vacancyId, request.Status, cancellationToken).ConfigureAwait(false);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpGet("positions")]
    public async Task<IActionResult> Positions(string clubId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetPositionsAsync(clubId, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpGet("positions/{positionId}")]
    public async Task<IActionResult> PositionById(string clubId, string positionId, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.GetPositionAsync(clubId, positionId, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition(string clubId, [FromBody] CreatePositionRequest request, CancellationToken cancellationToken)
    {
        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title
            : request.Name ?? string.Empty;
        var created = await _clubAdmin.CreatePositionAsync(clubId, new ClubPositionCreateDto
        {
            Title = title,
            CategoryId = request.CategoryId,
            Requirements = request.Requirements ?? new List<string>()
        }, cancellationToken).ConfigureAwait(false);
        return Ok(created);
    }

    [HttpPatch("positions/{positionId}")]
    public async Task<IActionResult> PatchPosition(string clubId, string positionId, [FromBody] PatchPositionRequest request, CancellationToken cancellationToken)
    {
        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title
            : request.Name ?? string.Empty;
        var updated = await _clubAdmin.PatchPositionAsync(clubId, positionId, new ClubPositionUpdateDto
        {
            Title = title,
            CategoryId = request.CategoryId,
            Requirements = request.Requirements ?? new List<string>()
        }, cancellationToken).ConfigureAwait(false);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("positions/{positionId}")]
    public async Task<IActionResult> DeletePosition(string clubId, string positionId, CancellationToken cancellationToken)
    {
        var ok = await _clubAdmin.DeletePositionAsync(clubId, positionId, cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("members")]
    public async Task<IActionResult> Members(string clubId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetMembersAsync(clubId, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpDelete("members/{memberId}")]
    public async Task<IActionResult> DeleteMember(string clubId, string memberId, CancellationToken cancellationToken)
    {
        var ok = await _clubAdmin.DeleteMemberAsync(clubId, memberId, cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("employees")]
    public async Task<IActionResult> Employees(string clubId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetEmployeesAsync(clubId, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(string clubId, [FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await _clubAdmin.CreateEmployeeAsync(clubId, request.StudentId, request.PositionId, cancellationToken).ConfigureAwait(false);
        return Ok(created);
    }

    [HttpPatch("employees/positions")]
    public async Task<IActionResult> PatchEmployeePositions(string clubId, [FromBody] UpdateEmployeePositionsRequest request, CancellationToken cancellationToken)
    {
        var changes = request.Changes.Select(c => new EmployeePositionChange
        {
            EmployeeId = c.EmployeeId,
            PositionId = c.PositionId
        }).ToList();
        await _clubAdmin.PatchEmployeePositionsAsync(clubId, changes, cancellationToken).ConfigureAwait(false);
        return Ok(new { updated = changes.Count });
    }

    [HttpDelete("employees/{employeeId}")]
    public async Task<IActionResult> DeleteEmployee(string clubId, string employeeId, CancellationToken cancellationToken)
    {
        var ok = await _clubAdmin.DeleteEmployeeAsync(clubId, employeeId, cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(string clubId, [FromQuery] string? status = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetEventsAsync(clubId, status, search, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPatch("events/{eventId}")]
    public async Task<IActionResult> PatchEvent(string clubId, string eventId, [FromBody] UpdateEventDto body, CancellationToken cancellationToken)
    {
        var updated = await _clubAdmin.PatchEventAsync(clubId, eventId, body, cancellationToken).ConfigureAwait(false);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpPost("events/proposals")]
    public async Task<IActionResult> EventProposal(string clubId, [FromBody] object body, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.CreateEventProposalAsync(clubId, body, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet("events/{eventId}/attendees")]
    public async Task<IActionResult> EventAttendees(string clubId, string eventId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _clubAdmin.GetEventAttendeesAsync(clubId, eventId, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(new { result.Items, result.Page, result.Limit, result.Total });
    }

    [HttpPatch("events/{eventId}/attendees/{attendeeId}")]
    public async Task<IActionResult> PatchAttendee(string clubId, string eventId, string attendeeId, [FromBody] AttendeePatchRequest request, CancellationToken cancellationToken)
    {
        var dto = await _clubAdmin.PatchEventAttendeeAsync(clubId, eventId, attendeeId, request.Entered, cancellationToken).ConfigureAwait(false);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpDelete("events/{eventId}/attendees/{attendeeId}")]
    public async Task<IActionResult> DeleteAttendee(string clubId, string eventId, string attendeeId, CancellationToken cancellationToken)
    {
        var ok = await _clubAdmin.DeleteEventAttendeeAsync(clubId, eventId, attendeeId, cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("profile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PatchProfile(string clubId, [FromForm] ClubProfilePatchRequest request, CancellationToken cancellationToken)
    {
        var result = await _clubAdmin.PatchProfileAsync(clubId, request.SocialLinks, request.FocusAreasJson, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> AddAnnouncement(string clubId, [FromBody] AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var created = await _clubAdmin.CreateAnnouncementAsync(clubId, request.Title, request.Message, cancellationToken).ConfigureAwait(false);
        return Ok(created);
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
        public List<string> ApplicationIds { get; set; } = new();
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

    public sealed class PatchInterviewSlotBody
    {
        public DateTimeOffset? StartsAt { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
        public string? VacancyId { get; set; }
        public int? Capacity { get; set; }
    }

    public sealed class CreateVacancyRequest
    {
        public string? Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? PositionId { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public bool Publish { get; set; } = true;
    }

    public sealed class UpdateVacancyRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public int? PositionId { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public bool? IsDraft { get; set; }
    }

    public sealed class CreatePositionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int CategoryId { get; set; }
        public List<string> Requirements { get; set; } = new();
    }

    public sealed class PatchPositionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int CategoryId { get; set; }
        public List<string> Requirements { get; set; } = new();
    }

    public sealed class CreateEmployeeRequest
    {
        public string StudentId { get; set; } = string.Empty;
        public string PositionId { get; set; } = string.Empty;
    }

    public sealed class UpdateEmployeePositionsRequest
    {
        public List<UpdateEmployeePositionItem> Changes { get; set; } = new();
    }

    public sealed class UpdateEmployeePositionItem
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string PositionId { get; set; } = string.Empty;
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
