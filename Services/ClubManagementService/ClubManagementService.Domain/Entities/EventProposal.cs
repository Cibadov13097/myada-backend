using System.Text.Json;

namespace ClubManagementService.Domain.Entities;

public sealed class EventProposal
{
    public int Id { get; set; }

    public int? ClubNumericId { get; set; }
    public string? ClubRouteKey { get; set; }
    public string ClubName { get; set; } = string.Empty;

    public string BodyJson { get; set; } = "{}";
    public string? ImageUrl { get; set; }

    public string Status { get; set; } = "pending_review";
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid SubmittedByUserId { get; set; }

    // When a proposal leaves the queue (approved/rejected/etc.), we archive it instead of hard-deleting.
    // Queue endpoints hide archived proposals by default.
    public DateTimeOffset? ArchivedAt { get; set; }

    public int? CreatedEventId { get; set; }
    public string? LastApproveAssignmentsJson { get; set; }

    public string ProposalId => Id.ToString();

    public JsonDocument BodyAsJson()
        => JsonDocument.Parse(string.IsNullOrWhiteSpace(BodyJson) ? "{}" : BodyJson);
}
