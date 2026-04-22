using System.Text.Json;

namespace ClubManagementService.Application.DTOs.Contracts;

public sealed class ClubProposalSubmissionDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? LogoFile { get; set; }
    public string? ConstitutionFile { get; set; }

    public string? PresidentStudentId { get; set; }
    public string? VicePresidentStudentId { get; set; }
    public JsonElement? OtherMembersJson { get; set; }
    public string? Commitment { get; set; }

    public string? ShortDesc { get; set; }
    public string? UniqueDesc { get; set; }
    public string? Goals { get; set; }
    public string? Activities { get; set; }
    public string? Alignment { get; set; }
    public string? Vision { get; set; }
}

public sealed class ClubProposalFilePart
{
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? ContentType { get; init; }
}

public sealed class EventProposalSubmitDto
{
    public string Name { get; set; } = string.Empty;
    public string DateTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Attendance { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Objectives { get; set; } = string.Empty;
    public List<EventProposalSubEventDto> SubEvents { get; set; } = new();
    public EventProposalLogisticsDto Logistics { get; set; } = new();
    public string? SubmittedByOrganization { get; set; }
}

public sealed class EventProposalSubEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Capacity { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public sealed class EventProposalLogisticsDto
{
    public bool AvSetup { get; set; }
    public bool Security { get; set; }
    public bool Catering { get; set; }
    public bool Cleaning { get; set; }
    public string? OtherNeeds { get; set; }
}

public sealed class ClubProposalRequirementsDto
{
    public List<string> Requirements { get; set; } = new();
    public DateTime? Deadline { get; set; }
}

public sealed class EventProposalAssignmentDto
{
    public int SubEventIndex { get; set; }
    public string BuildingId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
}

public sealed class EventProposalApproveDto
{
    public List<EventProposalAssignmentDto> Assignments { get; set; } = new();
}
