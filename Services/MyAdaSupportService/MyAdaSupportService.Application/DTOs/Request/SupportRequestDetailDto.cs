using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.DTOs.Request;


public class SupportRequestDetailDto
{
    public int Id { get; set; }
    public string TicketNo { get; set; }
    public string Description { get; set; }
    public string DescriptionFull { get; set; }
    public string Category { get; set; }
    public string Service { get; set; }
    public bool IsBuilding { get; set; }
    public int? BuildingId { get; set; }
    public bool IsRoom { get; set; }
    public int? RoomId { get; set; }
    public string? AreaDetails { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
    public bool IsUrgent { get; set; }
    public string Priority { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CancelReason { get; set; }
    public int? Rating { get; set; }

    public ReporterInfoDto? Reporter { get; set; }     // dispatcher/staff view
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<TimelineItemDto> Timeline { get; set; } = new();
    //public List<RequestMessageDto> Messages { get; set; } = new();
    public string? DispatcherInstructions { get; set; }
}

public class ReporterInfoDto
{
    public Guid MemberId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}



public class TimelineItemDto
{
    public string Step { get; set; }
    public string Detail { get; set; }
    public bool Done { get; set; }
    public DateTime CreatedAt { get; set; }
}
