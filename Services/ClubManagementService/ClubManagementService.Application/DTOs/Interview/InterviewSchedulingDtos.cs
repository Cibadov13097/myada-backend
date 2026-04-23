namespace ClubManagementService.Application.DTOs.Interview;

public sealed class InterviewSlotItemDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset EndAt { get; init; }
    public string Timezone { get; init; } = "UTC";
    public int Capacity { get; init; }
    public int BookedCount { get; init; }
    public int RemainingCapacity { get; init; }
    public string Status { get; init; } = "ACTIVE";
    public bool Bookable { get; init; }
    public string? LocationType { get; init; }
    public string? MeetingUrl { get; init; }
    public string? InterviewerName { get; init; }
}

public sealed class InterviewSlotListResult
{
    public bool NotFound { get; init; }
    public bool Forbidden { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<InterviewSlotItemDto> Slots { get; init; } = Array.Empty<InterviewSlotItemDto>();
}

public sealed class InterviewBookingResultDto
{
    public bool Forbidden { get; init; }
    public bool NotFound { get; init; }
    public bool Conflict { get; init; }
    public bool BadRequest { get; init; }
    public string? Message { get; init; }
    public string BookingId { get; init; } = string.Empty;
    public string InterviewId { get; init; } = string.Empty;
    public string SlotId { get; init; } = string.Empty;
    public string Status { get; init; } = "CONFIRMED";
    public DateTimeOffset? StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }
    public bool IsIdempotentReplay { get; init; }
}
