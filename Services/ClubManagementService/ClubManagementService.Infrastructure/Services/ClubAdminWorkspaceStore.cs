using System.Collections.Concurrent;

namespace ClubManagementService.Infrastructure.Services;

public sealed class ClubAdminWorkspaceStore
{
    private readonly ConcurrentDictionary<int, ClubWorkspaceState> _clubs = new();

    public ClubWorkspaceState GetOrCreate(int clubId) => _clubs.GetOrAdd(clubId, _ => new ClubWorkspaceState());
}

public sealed class ClubWorkspaceState
{
    public ConcurrentDictionary<Guid, MembershipApplicationRow> MembershipApplications { get; } = new();
    public ConcurrentDictionary<string, InterviewSlotRow> InterviewSlots { get; } = new();
    public ConcurrentDictionary<string, PositionRow> Positions { get; } = new();
    public ConcurrentDictionary<string, MemberRow> Members { get; } = new();
    public ConcurrentDictionary<string, EmployeeRow> Employees { get; } = new();
    public ConcurrentDictionary<string, AnnouncementRow> Announcements { get; } = new();
    public ConcurrentDictionary<string, AttendeeBucket> EventAttendees { get; } = new();

    public ConcurrentDictionary<string, ConcurrentQueue<ApplicationNoteRow>> ApplicationNotes { get; } = new();
}

public sealed class MembershipApplicationRow
{
    public Guid Id { get; init; }
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class InterviewSlotRow
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? VacancyId { get; set; }
    public int Capacity { get; set; }
}

public sealed class PositionRow
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class MemberRow
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}

public sealed class EmployeeRow
{
    public string Id { get; init; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
}

public sealed class AnnouncementRow
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AttendeeBucket
{
    public ConcurrentDictionary<string, AttendeeRow> Items { get; } = new();
}

public sealed class AttendeeRow
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public bool Entered { get; set; }
}

public sealed class ApplicationNoteRow
{
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
