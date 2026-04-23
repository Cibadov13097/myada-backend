namespace ClubManagementService.Application.DTOs.ClubAdmin;

public sealed class ClubProfileUploadHintsDto
{
    public bool HasLogo { get; init; }
    public bool HasBackground { get; init; }
}

public sealed class ClubAdminPagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
}

public sealed class ClubAdminDashboardDto
{
    public string ClubId { get; init; } = string.Empty;
    public ClubAdminDashboardStatsDto Stats { get; init; } = new();
    public IReadOnlyList<object> Activity { get; init; } = Array.Empty<object>();
}

public sealed class ClubAdminDashboardStatsDto
{
    public int ActiveMembers { get; init; }
    public int OpenVacancies { get; init; }
    public int NewApplications { get; init; }
    public int UpcomingEvents { get; init; }
}

public sealed class ClubAdminApplicationListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string ApplicationType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public ClubAdminMembershipApplicationBriefDto? Membership { get; init; }
    public ClubAdminJobApplicationBriefDto? Job { get; init; }
}

public sealed class ClubAdminMembershipApplicationBriefDto
{
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? LetterOfPurpose { get; init; }
    public string? Links { get; init; }
}

public sealed class ClubAdminJobApplicationBriefDto
{
    public string VacancyId { get; init; } = string.Empty;
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? VacancyTitle { get; init; }
}

public sealed class ClubAdminApplicationDetailDto
{
    public string Id { get; init; } = string.Empty;
    public string ApplicationType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public ClubAdminMembershipApplicationDetailDto? Membership { get; init; }
    public ClubAdminJobApplicationDetailDto? Job { get; init; }
    public IReadOnlyList<ClubAdminApplicationNoteDto> Notes { get; init; } = Array.Empty<ClubAdminApplicationNoteDto>();
}

public sealed class ClubAdminMembershipApplicationDetailDto
{
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? LetterOfPurpose { get; init; }
    public string? Links { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class ClubAdminJobApplicationDetailDto
{
    public string VacancyId { get; init; } = string.Empty;
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? VacancyTitle { get; init; }
    public string ExperienceLinks { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string? InterviewSlotId { get; init; }
}

public sealed class ClubAdminApplicationNoteDto
{
    public string Text { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class ClubAdminApplicationActionResultDto
{
    public string ApplicationId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class ClubAdminVacancyAdminDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PostedBy { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? PositionId { get; init; }
    public DateTime? ApplicationDeadline { get; init; }
    public bool IsDraft { get; init; }
}

public sealed class ClubAdminEventAdminDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Location { get; init; } = string.Empty;
    public int SeatLimit { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class ClubAdminInterviewSlotDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    public string? VacancyId { get; init; }
    public int Capacity { get; init; }
    public bool IsPublished { get; init; } = true;
}

public sealed class ClubAdminPositionDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    /// <summary>Same as title; kept for older clients.</summary>
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public IReadOnlyList<string> Requirements { get; init; } = Array.Empty<string>();
}

public sealed class ClubPositionCreateDto
{
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<string> Requirements { get; set; } = new();
}

public sealed class ClubPositionUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<string> Requirements { get; set; } = new();
}

public sealed class ClubAdminMemberDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}

public sealed class ClubAdminEmployeeDto
{
    public string Id { get; init; } = string.Empty;
    public string ClubId { get; init; } = string.Empty;
    public string StudentId { get; init; } = string.Empty;
    public string PositionId { get; init; } = string.Empty;
}

public sealed class ClubAdminAnnouncementDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class ClubAdminAttendeeDto
{
    public string Id { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public bool Entered { get; init; }
    public string TicketId { get; init; } = string.Empty;
    public string RegisteredAt { get; init; } = string.Empty;
}
