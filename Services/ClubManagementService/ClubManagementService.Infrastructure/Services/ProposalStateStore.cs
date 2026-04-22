using System.Collections.Concurrent;
using System.Globalization;
using ClubManagementService.Application.DTOs.Contracts;

namespace ClubManagementService.Infrastructure.Services;

public sealed class ProposalStateStore
{
    private int _clubProposalSeq;
    private int _eventProposalSeq;

    private readonly ConcurrentDictionary<string, ClubProposalRecord> _clubProposals =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _clubInternalIdToProposalId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, EventProposalRecord> _eventProposals =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _eventInternalIdToProposalId =
        new(StringComparer.OrdinalIgnoreCase);

    private ClubProposalRequirementsDto _requirements = new();

    public string NextClubProposalId()
    {
        var n = Interlocked.Increment(ref _clubProposalSeq);
        return $"PROP-{DateTime.UtcNow.Year}-{n:000}";
    }

    public string NextEventProposalId()
    {
        var n = Interlocked.Increment(ref _eventProposalSeq);
        return n.ToString(CultureInfo.InvariantCulture);
    }

    public void AddClubProposal(ClubProposalRecord record)
    {
        _clubProposals[record.ProposalId] = record;
        _clubInternalIdToProposalId[record.Id] = record.ProposalId;
    }

    public ClubProposalRecord? FindClubProposal(string proposalIdOrInternalId)
    {
        if (_clubProposals.TryGetValue(proposalIdOrInternalId, out var byBusiness))
            return byBusiness;
        if (_clubInternalIdToProposalId.TryGetValue(proposalIdOrInternalId, out var pid) &&
            _clubProposals.TryGetValue(pid, out var byInternal))
            return byInternal;
        return null;
    }

    public IReadOnlyList<ClubProposalRecord> AllClubProposals() => _clubProposals.Values.OrderByDescending(x => x.SubmittedAt).ToList();

    public ClubProposalRequirementsDto GetRequirements() => new()
    {
        Requirements = _requirements.Requirements.ToList(),
        Deadline = _requirements.Deadline
    };

    public ClubProposalRequirementsDto SetRequirements(ClubProposalRequirementsDto dto)
    {
        _requirements = new ClubProposalRequirementsDto
        {
            Requirements = dto.Requirements?.ToList() ?? new List<string>(),
            Deadline = dto.Deadline
        };
        return GetRequirements();
    }

    public void AddEventProposal(EventProposalRecord record)
    {
        _eventProposals[record.ProposalId] = record;
        _eventInternalIdToProposalId[record.Id] = record.ProposalId;
    }

    public EventProposalRecord? FindEventProposal(string proposalIdOrInternalId)
    {
        if (_eventProposals.TryGetValue(proposalIdOrInternalId, out var a))
            return a;
        if (_eventInternalIdToProposalId.TryGetValue(proposalIdOrInternalId, out var pid) &&
            _eventProposals.TryGetValue(pid, out var b))
            return b;
        return null;
    }

    public IReadOnlyList<EventProposalRecord> AllEventProposals() =>
        _eventProposals.Values.OrderByDescending(x => x.SubmittedAt).ToList();
}

public sealed class ClubProposalRecord
{
    public string Id { get; init; } = string.Empty;
    public string ProposalId { get; init; } = string.Empty;
    public string SubmittedByUserId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDesc { get; set; }
    public string? UniqueDesc { get; set; }
    public string? Goals { get; set; }
    public string? Activities { get; set; }
    public string? Alignment { get; set; }
    public string? Vision { get; set; }
    public string? Category { get; set; }
    public string? Commitment { get; set; }
    public List<PrimaryOfficer> PrimaryOfficers { get; set; } = new();
    public List<OtherOfficer> OtherMembers { get; set; } = new();
    public string Status { get; set; } = "pending_review";
    public DateTimeOffset SubmittedAt { get; init; }
    public string LogoUrl { get; init; } = string.Empty;
    public ConstitutionDoc ConstitutionDoc { get; init; } = new();
    public int? ApprovedClubId { get; set; }
}

public sealed class PrimaryOfficer
{
    public string Role { get; init; } = string.Empty;
    public string StudentId { get; init; } = string.Empty;
}

public sealed class OtherOfficer
{
    public string StudentId { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string Status { get; init; } = "Pending";
}

public sealed class ConstitutionDoc
{
    public string Name { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public string Url { get; init; } = string.Empty;
}

public sealed class EventProposalRecord
{
    public string Id { get; init; } = string.Empty;
    public string ProposalId { get; init; } = string.Empty;
    public int? ClubNumericId { get; init; }
    public string? ClubRouteKey { get; init; }
    public string? ClubName { get; init; }
    public EventProposalSubmitDto Body { get; init; } = new();
    public string Status { get; set; } = "pending_review";
    public DateTimeOffset SubmittedAt { get; init; }
    public Guid SubmittedByUserId { get; init; }
    public int? CreatedEventId { get; set; }
    public List<EventProposalAssignmentDto>? LastApproveAssignments { get; set; }
}
