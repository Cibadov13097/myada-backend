using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobVacancy;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IClubAdminService
{
    Task<ClubAdminDashboardDto?> GetDashboardAsync(string clubRouteKey, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminApplicationListItemDto>> GetApplicationsAsync(
        string clubRouteKey,
        string? type,
        string? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ClubAdminApplicationDetailDto?> GetApplicationAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default);

    Task<ClubAdminApplicationActionResultDto?> ApproveApplicationAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default);
    Task<ClubAdminApplicationActionResultDto?> RejectApplicationAsync(string clubRouteKey, string applicationId, string? reason, CancellationToken cancellationToken = default);
    Task<ClubAdminApplicationActionResultDto?> RequestChangesAsync(string clubRouteKey, string applicationId, string? changes, CancellationToken cancellationToken = default);
    Task<ClubAdminApplicationActionResultDto?> InviteToInterviewAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default);
    Task<ClubAdminApplicationNoteDto?> AddApplicationNoteAsync(string clubRouteKey, string applicationId, string note, CancellationToken cancellationToken = default);
    Task<BulkDecisionResultDto> BulkDecisionAsync(string clubRouteKey, IReadOnlyList<string> applicationIds, string decision, string? reason, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminInterviewSlotDto>> GetInterviewSlotsAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminPagedResult<ClubAdminInterviewSlotDto>> GenerateInterviewSlotsAsync(string clubRouteKey, GenerateInterviewSlotsInput input, CancellationToken cancellationToken = default);
    Task<ClubAdminInterviewSlotDto?> PatchInterviewSlotAsync(string clubRouteKey, string slotId, PatchInterviewSlotInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteInterviewSlotAsync(string clubRouteKey, string slotId, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminVacancyAdminDto>> GetVacanciesAsync(string clubRouteKey, string? status, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminVacancyAdminDto> CreateVacancyAsync(string clubRouteKey, CreateJobVacancyDto dto, CancellationToken cancellationToken = default);
    Task<ClubAdminVacancyAdminDto?> PatchVacancyAsync(string clubRouteKey, string vacancyId, UpdateJobVacancyDto dto, CancellationToken cancellationToken = default);
    Task<ClubAdminVacancyAdminDto?> PatchVacancyStatusAsync(string clubRouteKey, string vacancyId, string status, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminPositionDto>> GetPositionsAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminPositionDto?> GetPositionAsync(string clubRouteKey, string positionId, CancellationToken cancellationToken = default);
    Task<ClubAdminPositionDto> CreatePositionAsync(string clubRouteKey, ClubPositionCreateDto dto, CancellationToken cancellationToken = default);
    Task<ClubAdminPositionDto?> PatchPositionAsync(string clubRouteKey, string positionId, ClubPositionUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeletePositionAsync(string clubRouteKey, string positionId, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminMemberDto>> GetMembersAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default);
    Task<bool> DeleteMemberAsync(string clubRouteKey, string memberId, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminEmployeeDto>> GetEmployeesAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminEmployeeDto> CreateEmployeeAsync(string clubRouteKey, string studentId, string positionId, CancellationToken cancellationToken = default);
    Task PatchEmployeePositionsAsync(string clubRouteKey, IReadOnlyList<EmployeePositionChange> changes, CancellationToken cancellationToken = default);
    Task<bool> DeleteEmployeeAsync(string clubRouteKey, string employeeId, CancellationToken cancellationToken = default);

    Task<ClubAdminPagedResult<ClubAdminEventAdminDto>> GetEventsAsync(string clubRouteKey, string? status, string? search, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminEventAdminDto?> PatchEventAsync(string clubRouteKey, string eventId, UpdateEventDto dto, CancellationToken cancellationToken = default);
    Task<EventProposalResultDto?> CreateEventProposalAsync(string clubRouteKey, EventProposalSubmitDto body, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ClubAdminPagedResult<ClubAdminAttendeeDto>> GetEventAttendeesAsync(string clubRouteKey, string eventId, int page, int limit, CancellationToken cancellationToken = default);
    Task<ClubAdminAttendeeDto?> PatchEventAttendeeAsync(string clubRouteKey, string eventId, string attendeeId, bool entered, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAttendeeAsync(string clubRouteKey, string eventId, string attendeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClubResourceItemDto>> GetFilesAsync(string clubRouteKey, CancellationToken cancellationToken = default);
    Task<ClubResourceItemDto?> GetFileAsync(string clubRouteKey, string fileId, CancellationToken cancellationToken = default);
    Task<ClubResourceItemDto?> CreateFileAsync(string clubRouteKey, ClubAdminCreateFileInput input, CancellationToken cancellationToken = default);
    Task<ClubResourceItemDto?> UpdateFileAsync(string clubRouteKey, string fileId, ClubAdminUpdateFileInput input, CancellationToken cancellationToken = default);
    Task<ClubResourceItemDto?> DeleteFileAsync(string clubRouteKey, string fileId, CancellationToken cancellationToken = default);

    Task<ProfilePatchResultDto> PatchProfileAsync(
        string clubRouteKey,
        string? socialLinks,
        string? focusAreasJson,
        string? email,
        string? resourcesJson,
        ClubProfileUploadHintsDto? uploadHints,
        CancellationToken cancellationToken = default);
    Task<ClubAdminAnnouncementDto> CreateAnnouncementAsync(string clubRouteKey, string title, string message, CancellationToken cancellationToken = default);
}

public sealed class BulkDecisionResultDto
{
    public int Updated { get; init; }
}

public sealed class GenerateInterviewSlotsInput
{
    public string Date { get; init; } = string.Empty;
    /// <summary>Optional inclusive end date (same time window each day). If null, only <see cref="Date"/> is used.</summary>
    public string? EndDate { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public int InterviewLengthMinutes { get; init; }
    public string? VacancyId { get; init; }
}

public sealed class PatchInterviewSlotInput
{
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? VacancyId { get; init; }
    public int? Capacity { get; init; }
    public bool? IsPublished { get; init; }
}

public sealed class EmployeePositionChange
{
    public string EmployeeId { get; init; } = string.Empty;
    public string PositionId { get; init; } = string.Empty;
}

public sealed class EventProposalResultDto
{
    public string ProposalId { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = "pending_review";
    public string ClubId { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int? BuildingId { get; init; }
    public int? RoomId { get; init; }
}

public sealed class ProfilePatchResultDto
{
    public string ClubId { get; init; } = string.Empty;

    /// <summary><c>true</c> when profile metadata and/or uploaded image fields were written.</summary>
    public bool Updated { get; init; }

    /// <summary>Option A (immediate publish): always false. Option B would set true when changes await approval.</summary>
    public bool PendingReview { get; init; }

    public List<ClubFocusAreaItemDto> FocusAreas { get; init; } = new();

    public string? ProfileImageUrl { get; init; }

    public string? BackgroundImageUrl { get; init; }

    public string? Email { get; init; }

    public ClubSocialLinksDto? SocialLinks { get; init; }

    public List<ClubResourceItemDto> Resources { get; init; } = new();
}
