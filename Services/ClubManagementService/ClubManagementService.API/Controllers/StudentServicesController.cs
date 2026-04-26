using Asp.Versioning;
using ClubManagementService.API.Models;
using ClubManagementService.API.Responses;
using ClubManagementService.API.Security;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/student-services")]
[ApiController]
[Authorize]
[Tags("Student Services")]
public class StudentServicesController : ControllerBase
{
    private readonly IClubService _clubService;
    private readonly IEventService _eventService;
    private readonly IClubNotificationService _notificationService;
    private readonly IProposalContractService _proposalContract;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClubManagementDbContext _db;

    public StudentServicesController(
        IClubService clubService,
        IEventService eventService,
        IClubNotificationService notificationService,
        IProposalContractService proposalContract,
        IImageStorageService imageStorageService,
        ClubManagementDbContext db)
    {
        _clubService = clubService;
        _eventService = eventService;
        _notificationService = notificationService;
        _proposalContract = proposalContract;
        _imageStorageService = imageStorageService;
        _db = db;
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        return Ok(new { cards = Array.Empty<object>() });
    }

    [HttpGet("clubs")]
    public async Task<IActionResult> Clubs([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var clubs = await _clubService.GetAllAsync().ConfigureAwait(false);
        var filtered = clubs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var items = filtered
            .Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1))
            .Take(Math.Max(limit, 1))
            .Select(c =>
            {
                var profileImageUrl = ClubImageResponse.Normalize(c.ProfileImageUrl);
                var backgroundImageUrl = ClubImageResponse.Normalize(c.BackgroundImageUrl);
                var backgroundImage = ClubImageResponse.ResolveDisplayImage(backgroundImageUrl);
                return new
                {
                    profileImageUrl,
                    backgroundImageUrl,
                    id = c.Id,
                    name = c.Name,
                    category = c.CategoryName ?? "General",
                    status = string.Equals(c.Status.ToString(), "Approved", StringComparison.OrdinalIgnoreCase) ? "Active" :
                             string.Equals(c.Status.ToString(), "Archived", StringComparison.OrdinalIgnoreCase) ? "Inactive" :
                             c.Status.ToString(),
                    members = 0,
                    president = (string?)null,
                    presidentId = (string?)null,
                    backgroundImage,
                    image = backgroundImage,
                    proposedProfileImageUrl = ClubImageResponse.Normalize(c.ProposedProfileImageUrl)
                };
            })
            .ToList();

        return Ok(new { items, total = filtered.Count(), page, limit });
    }

    [HttpGet("clubs/{clubId:int}")]
    public async Task<IActionResult> ClubById(int clubId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var club = await _clubService.GetByIdAsync(clubId);
        return club == null ? NotFound() : Ok(club);
    }

    [HttpGet("clubs/proposals")]
    public Task<IActionResult> ClubProposals(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        ClubProposalsCore(status, search, page, limit, cancellationToken);

    private async Task<IActionResult> ClubProposalsCore(string? status, string? search, int page, int limit, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var data = await _proposalContract.ListClubProposalsAsync(status, search, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpGet("clubs/proposals/{proposalId}")]
    public async Task<IActionResult> ClubProposalById(string proposalId, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var data = await _proposalContract.GetClubProposalAsync(proposalId, cancellationToken).ConfigureAwait(false);
        return data == null ? NotFound() : Ok(data);
    }

    [HttpPost("clubs/proposals/{proposalId}/approve")]
    public async Task<IActionResult> ApproveClubProposal(string proposalId, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var data = await _proposalContract.ApproveClubProposalAsync(proposalId, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Approved",
            Message = $"Club proposal '{proposalId}' was approved.",
            Source = "club-management",
            Action = "club_proposal.approved",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpPost("clubs/proposals/{proposalId}/reject")]
    public async Task<IActionResult> RejectClubProposal(string proposalId, [FromBody] ReasonRequest request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var data = await _proposalContract.RejectClubProposalAsync(proposalId, request.Reason, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Rejected",
            Message = $"Club proposal '{proposalId}' was rejected. Reason: {request.Reason}",
            Source = "club-management",
            Action = "club_proposal.rejected",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpPost("clubs/proposals/{proposalId}/request-revision")]
    public async Task<IActionResult> RequestClubProposalRevision(string proposalId, [FromBody] ChangesRequest request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var data = await _proposalContract.RequestClubProposalRevisionAsync(proposalId, request.Changes, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Revision Requested",
            Message = $"Revision requested for club proposal '{proposalId}'.",
            Source = "club-management",
            Action = "club_proposal.revision_requested",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpGet("club-proposal-requirements")]
    public IActionResult ClubProposalRequirements() => Ok(_proposalContract.GetClubProposalRequirements());

    [HttpPut("club-proposal-requirements")]
    public async Task<IActionResult> UpdateClubProposalRequirements([FromBody] ClubProposalRequirementsDto request)
    {
        var updated = _proposalContract.SetClubProposalRequirements(request);
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Proposal Requirements Updated",
            Message = $"Club proposal requirements were updated ({updated.Requirements.Count} requirement items).",
            Source = "club-management",
            Action = "club_proposal_requirements.updated",
            EntityType = "club_proposal_requirements",
            EntityId = "active",
            ActorUserId = GetCurrentUserIdOrDefault()
        }).ConfigureAwait(false);

        return Ok(updated);
    }

    [HttpPost("events/proposals")]
    [Consumes("application/json", "multipart/form-data")]
    public async Task<IActionResult> CreateStaffEventProposal(CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        var (body, uploadedImageUrl, errorResult) = await TryReadEventProposalBodyAsync(
            "student-services/event-proposals",
            cancellationToken).ConfigureAwait(false);
        if (errorResult != null)
            return errorResult;

        try
        {
            var actor = GetCurrentUserIdOrDefault();
            var result = await _proposalContract.SubmitStaffEventProposalAsync(body!, actor, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                await TryDeleteStoredFileAsync(uploadedImageUrl, cancellationToken).ConfigureAwait(false);
                return BadRequest(result.ErrorBody ?? new { message = result.ErrorMessage });
            }

            return Ok(result.Payload);
        }
        catch
        {
            await TryDeleteStoredFileAsync(uploadedImageUrl, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    [HttpGet("events/proposals")]
    public Task<IActionResult> EventProposals(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        EventProposalsCore(search, status, page, limit, cancellationToken);

    private async Task<IActionResult> EventProposalsCore(string? search, string? status, int page, int limit, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var data = await _proposalContract.ListEventProposalsAsync(status, search, page, limit, cancellationToken).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpGet("events/proposals/{proposalId}")]
    public async Task<IActionResult> EventProposalById(string proposalId, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var data = await _proposalContract.GetEventProposalAsync(proposalId, cancellationToken).ConfigureAwait(false);
        return data == null ? NotFound() : Ok(data);
    }

    [HttpPost("events/proposals/{proposalId}/approve")]
    public async Task<IActionResult> ApproveEventProposal(string proposalId, [FromBody] EventProposalApproveDto? request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var body = request ?? new EventProposalApproveDto();
        var data = await _proposalContract.ApproveEventProposalAsync(proposalId, body, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Approved",
            Message = $"Event proposal '{proposalId}' was approved.",
            Source = "club-management",
            Action = "event_proposal.approved",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpPost("events/proposals/{proposalId}/reject")]
    public async Task<IActionResult> RejectEventProposal(string proposalId, [FromBody] ReasonRequest request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var data = await _proposalContract.RejectEventProposalAsync(proposalId, request.Reason, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Rejected",
            Message = $"Event proposal '{proposalId}' was rejected. Reason: {request.Reason}",
            Source = "club-management",
            Action = "event_proposal.rejected",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpPost("events/proposals/{proposalId}/request-revision")]
    public async Task<IActionResult> RequestEventProposalRevision(string proposalId, [FromBody] ChangesRequest request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var actor = GetCurrentUserIdOrDefault();
        var data = await _proposalContract.RequestEventProposalRevisionAsync(proposalId, request.Changes, actor, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Proposal Revision Requested",
            Message = $"Revision requested for event proposal '{proposalId}'.",
            Source = "club-management",
            Action = "event_proposal.revision_requested",
            EntityType = "event_proposal",
            EntityId = proposalId,
            ActorUserId = actor
        }).ConfigureAwait(false);
        return Ok(data);
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var events = await _eventService.GetAllAsync();
        return Ok(new { items = events });
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var created = await _eventService.CreateAsync(new CreateEventDto
        {
            ClubId = request.ClubId,
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
    public async Task<IActionResult> PatchEvent(int eventId, [FromBody] UpdateEventDto body)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        try
        {
            var updated = await _eventService.UpdateAsync(eventId, body);
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
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("events/{eventId:int}")]
    public async Task<IActionResult> EventById(int eventId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        var evt = await _eventService.GetByIdAsync(eventId);
        return evt == null ? NotFound() : Ok(evt);
    }

    [HttpDelete("events/{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        await _eventService.DeleteAsync(eventId);
        return NoContent();
    }

    [HttpPatch("clubs/{clubId:int}")]
    public async Task<IActionResult> PatchClub(int clubId, [FromBody] UpdateClubRequest request)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        var existing = await _clubService.GetByIdAsync(clubId).ConfigureAwait(false);
        if (existing == null) return NotFound();

        var updated = await _clubService.UpdateAsync(clubId, new UpdateClubDto
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name,
            Description = request.Description ?? existing.Description,
            Status = ParseClubStatus(request.Status ?? existing.Status.ToString())
        });

        return Ok(updated);
    }

    [HttpPost("clubs/{clubId:int}/profile-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadClubProfileImage(int clubId, [FromForm] UploadClubProfileImageRequest request, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        if (request.LogoFile == null || request.LogoFile.Length <= 0)
            return BadRequest(new { message = "logoFile is required." });

        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club == null)
            return NotFound();

        var ext = Path.GetExtension(request.LogoFile.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
        var safeExt = ext.Length > 10 ? ".png" : ext;

        var relDir = Path.Combine("uploads", "clubs", clubId.ToString(CultureInfo.InvariantCulture));
        var absDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relDir);
        Directory.CreateDirectory(absDir);

        var fileName = $"logo-proposed-{Guid.NewGuid():N}{safeExt}";
        var absPath = Path.Combine(absDir, fileName);
        await using (var fs = System.IO.File.Create(absPath))
            await request.LogoFile.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

        club.ProposedProfileImageUrl = "/" + Path.Combine(relDir, fileName).Replace("\\", "/");
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await _clubService.GetByIdAsync(clubId).ConfigureAwait(false);
        return dto == null ? Ok(new { clubId }) : Ok(dto);
    }

    [HttpPost("clubs/{clubId:int}/profile-image/approve")]
    public async Task<IActionResult> ApproveClubProfileImage(int clubId, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(club.ProposedProfileImageUrl))
        {
            club.ProfileImageUrl = club.ProposedProfileImageUrl;
            club.ProposedProfileImageUrl = null;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

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

        var dto = await _clubService.GetByIdAsync(clubId).ConfigureAwait(false);
        return dto == null ? Ok(new { clubId, approved = true }) : Ok(dto);
    }

    [HttpGet("clubs/{clubId:int}/members")]
    public IActionResult ClubMembers(int clubId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        return Ok(new { items = Array.Empty<object>() });
    }

    [HttpPatch("clubs/{clubId:int}/members/{memberId}")]
    public async Task<IActionResult> PatchClubMember(int clubId, string memberId, [FromBody] object body)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
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
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
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
    public IActionResult ClubEmployees(int clubId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
        return Ok(new { items = Array.Empty<object>() });
    }

    [HttpPost("clubs/{clubId:int}/employees")]
    public async Task<IActionResult> CreateClubEmployee(int clubId, [FromBody] CreateClubEmployeeRequest body, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        if (!Guid.TryParse(body.UserId, out var userId) || userId == Guid.Empty)
            return BadRequest(new { message = "userId must be a valid GUID." });

        int? positionId = null;
        if (!string.IsNullOrWhiteSpace(body.PositionId))
        {
            if (!int.TryParse(body.PositionId, out var pid) || pid <= 0)
                return BadRequest(new { message = "positionId must be a valid integer id." });
            positionId = pid;
        }

        var existing = await _db.ClubEmployees
            .FirstOrDefaultAsync(e => e.ClubId == clubId && e.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing == null)
        {
            existing = new ClubEmployee { ClubId = clubId, UserId = userId, PositionId = positionId };
            _db.ClubEmployees.Add(existing);
        }
        else
        {
            existing.PositionId = positionId;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var positionTitle = positionId.HasValue
            ? await _db.ClubPositions
                .AsNoTracking()
                .Where(p => p.ClubId == clubId && p.Id == positionId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;

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

        return Ok(new
        {
            employeeId = existing.Id.ToString(),
            clubId = clubId.ToString(),
            userId = existing.UserId.ToString("D"),
            positionId = existing.PositionId?.ToString(),
            positionTitle,
            positionName = positionTitle
        });
    }

    [HttpPatch("clubs/{clubId:int}/employees/{employeeId}")]
    public async Task<IActionResult> PatchClubEmployee(int clubId, string employeeId, [FromBody] PatchClubEmployeeRequest body, CancellationToken cancellationToken)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;

        if (!int.TryParse(employeeId, out var eid) || eid <= 0)
            return BadRequest(new { message = "Invalid employeeId." });

        int? positionId = null;
        if (!string.IsNullOrWhiteSpace(body.PositionId))
        {
            if (!int.TryParse(body.PositionId, out var pid) || pid <= 0)
                return BadRequest(new { message = "positionId must be a valid integer id." });
            positionId = pid;
        }

        var existing = await _db.ClubEmployees
            .FirstOrDefaultAsync(e => e.Id == eid && e.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (existing == null)
            return NotFound();

        existing.PositionId = positionId;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var positionTitle = positionId.HasValue
            ? await _db.ClubPositions
                .AsNoTracking()
                .Where(p => p.ClubId == clubId && p.Id == positionId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;

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

        return Ok(new
        {
            employeeId = existing.Id.ToString(),
            clubId = clubId.ToString(),
            userId = existing.UserId.ToString("D"),
            positionId = existing.PositionId?.ToString(),
            positionTitle,
            positionName = positionTitle
        });
    }

    public sealed class CreateClubEmployeeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string? PositionId { get; set; }
    }

    public sealed class PatchClubEmployeeRequest
    {
        public string? PositionId { get; set; }
    }

    [HttpDelete("clubs/{clubId:int}/employees/{employeeId}")]
    public async Task<IActionResult> DeleteClubEmployee(int clubId, string employeeId)
    {
        var denied = EnsureStudentServices();
        if (denied != null) return denied;
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
        // UI sends Active/Inactive. Map to our persisted enum.
        if (string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase))
            return ClubStatus.Approved;
        if (string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase))
            return ClubStatus.Archived;
        return Enum.TryParse<ClubStatus>(value, true, out var status) ? status : ClubStatus.Pending;
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private async Task<string?> UploadFileIfPresentAsync(string directory, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return null;

        if (!_imageStorageService.IsSupportedImage(file.FileName))
            return string.Empty;

        await using var stream = file.OpenReadStream();
        return await _imageStorageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            directory,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task TryDeleteStoredFileAsync(string? fileUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        try
        {
            await _imageStorageService.DeleteFileAsync(fileUrl, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Failed cleanup should not mask the original submit error.
        }
    }

    private async Task<(EventProposalSubmitDto? Body, string? UploadedImageUrl, IActionResult? ErrorResult)> TryReadEventProposalBodyAsync(
        string uploadDirectory,
        CancellationToken cancellationToken)
    {
        if (EventProposalRequestParser.IsMultipartRequest(Request))
        {
            IFormCollection form;
            try
            {
                form = HttpContext.Features.Get<IFormFeature>()?.Form
                       ?? await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                return (null, null, BadRequest(new
                {
                    message = "Invalid multipart form data. Use multipart/form-data for file uploads and let the client set the boundary automatically."
                }));
            }

            try
            {
                var multipart = EventProposalRequestParser.ToMultipartForm(form);
                var body = multipart.ToSubmissionDto();
                var uploadedImageUrl = await UploadFileIfPresentAsync(uploadDirectory, multipart.ImageFile, cancellationToken).ConfigureAwait(false);
                if (uploadedImageUrl == string.Empty)
                {
                    return (null, null, BadRequest(new { message = "imageFile has an unsupported file type." }));
                }

                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                    body.ImageUrl = uploadedImageUrl;

                return (body, uploadedImageUrl, null);
            }
            catch (JsonException)
            {
                return (null, null, BadRequest(new
                {
                    message = "Invalid multipart form data. subEventsJson must be a JSON array and logisticsJson must be a JSON object."
                }));
            }
        }

        if (EventProposalRequestParser.IsJsonRequest(Request))
        {
            try
            {
                return (await EventProposalRequestParser.ReadJsonBodyAsync(Request, cancellationToken).ConfigureAwait(false), null, null);
            }
            catch (JsonException)
            {
                return (null, null, BadRequest(new
                {
                    message = "Invalid JSON request body. Use multipart/form-data for direct image uploads, or application/json when imageUrl is already available."
                }));
            }
        }

        return (null, null, StatusCode(StatusCodes.Status415UnsupportedMediaType, new
        {
            message = "Unsupported content type. Use multipart/form-data for direct image uploads, or application/json when imageUrl is already available."
        }));
    }

    private IActionResult? EnsureStudentServices()
    {
        // Student Services endpoints must not be callable by club admins or regular users.
        return User.IsMyAdaAdministrator() ? null : Forbid();
    }

    public sealed class ReasonRequest { public string Reason { get; set; } = string.Empty; }
    public sealed class ChangesRequest { public string Changes { get; set; } = string.Empty; }

    public sealed class CreateEventRequest
    {
        public int ClubId { get; set; } = 1;
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
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
    }

    public sealed class UploadClubProfileImageRequest
    {
        public IFormFile? LogoFile { get; set; }
    }
}
