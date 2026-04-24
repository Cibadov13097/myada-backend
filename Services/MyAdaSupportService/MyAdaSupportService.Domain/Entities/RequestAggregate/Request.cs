using MyAdaSupportService.Domain.Entities.BaseAggregate;
using MyAdaSupportService.Domain.Enums;
using System.Net.Mail;

namespace MyAdaSupportService.Domain.Entities.RequestAggregate;

public class Request : BaseAuditedEntity
{
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Description { get; set; }= string.Empty;

    public bool IsBuilding { get; set; }
    public int? BuildingId { get; set; }

    public bool IsRoom { get; set; }
    public int? RoomId { get; set; }

    public string? AreaDetails { get; set; }

    public RequestStatus Status { get; set; }

    public string? ImgUrl { get; set; }

    public Guid? StaffId { get; set; }

    public DateTime? AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool IsUrgent { get; set; }

    public string? DispatcherInstructions { get; set; }

    public string? CancelReason { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
