namespace MyAdaSupportService.Application.DTOs.Request;

public class SupportRequestListItemDto
{
    public int Id { get; set; }
    public string TicketNo { get; set; }
    public string Title { get; set; }             // can be category name
    public string Description { get; set; }
    public string Category { get; set; }
    public string Service { get; set; }           // IT | FM
    public bool IsBuilding { get; set; }
    public int? BuildingId { get; set; }
    public bool IsRoom { get; set; }
    public int? RoomId { get; set; }
    public string? AreaDetails { get; set; }
    public string Location { get; set; }
    public string Status { get; set; }
    public string Urgency { get; set; }
    public bool IsUrgent { get; set; }
    public int? AssignedStaffId { get; set; }
    public string? AssignedTo { get; set; }
    public bool Unseen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}