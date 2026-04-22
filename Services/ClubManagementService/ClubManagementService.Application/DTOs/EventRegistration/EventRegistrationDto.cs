namespace ClubManagementService.Application.DTOs.EventRegistration;

public class EventRegistrationDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TicketId { get; set; } = string.Empty;
    public string RegistrationJwt { get; set; } = string.Empty;
    public string EventNameSnapshot { get; set; } = string.Empty;
    public string? EventImageUrlSnapshot { get; set; }
    public DateTime EventStartTimeSnapshot { get; set; }
    public DateTime EventEndTimeSnapshot { get; set; }
    public string EventLocationSnapshot { get; set; } = string.Empty;
    public int EventSeatLimitSnapshot { get; set; }

    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInBy { get; set; }
    public int ScanCount { get; set; }
    public DateTime? LastScannedAt { get; set; }
}

public class CreateEventRegistrationDto
{
    public int EventId { get; set; }
    public Guid UserId { get; set; }
}

public class UpdateEventRegistrationDto
{
    public int EventId { get; set; }
}
