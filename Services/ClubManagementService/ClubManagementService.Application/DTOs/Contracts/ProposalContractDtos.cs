using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ClubManagementService.Application.DTOs.Contracts;

public sealed class ClubProposalSubmissionDto
{
    [RegularExpression(@"^\d{9}$", ErrorMessage = "OrganizationId must be exactly 9 digits.")]
    [JsonPropertyName("organizationId")]
    public string? OrganizationId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? LogoFile { get; set; }
    public ClubProposalFilePart? LogoFilePart { get; set; }
    public string? ConstitutionFile { get; set; }
    public ClubProposalFilePart? ConstitutionFilePart { get; set; }

    [RegularExpression(@"^\d{9}$", ErrorMessage = "PresidentStudentId must be exactly 9 digits.")]
    public string? PresidentStudentId { get; set; }
    [RegularExpression(@"^\d{9}$", ErrorMessage = "VicePresidentStudentId must be exactly 9 digits.")]
    public string? VicePresidentStudentId { get; set; }
    public List<string> OtherMembersJson { get; set; } = new();
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
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Objectives { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
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
