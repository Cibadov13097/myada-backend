using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Application.DTOs.Event;

public class EventDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int SeatLimit { get; set; }
    public string Requirements { get; set; } = null!;
    public string Prerequisites { get; set; } = null!;
    public string Notes { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
    public ClubStatus Status { get; set; }
}

public class CreateEventDto
{
    public int ClubId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int SeatLimit { get; set; }
    public string Requirements { get; set; } = null!;
    public string Prerequisites { get; set; } = null!;
    public string Notes { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
}

public class UpdateEventDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = null!;
    public int SeatLimit { get; set; }
    public string Requirements { get; set; } = null!;
    public string Prerequisites { get; set; } = null!;
    public string Notes { get; set; } = null!;
    public ClubStatus Status { get; set; }
}
