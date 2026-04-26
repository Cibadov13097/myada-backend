using LostFoundService.Domain.Common;
using LostFoundService.Domain.Enums;

namespace LostFoundService.Domain.Entities;

public class LostFoundItem : AuditableEntity
{
    public LostFoundItemType Type { get; set; } = LostFoundItemType.Lost;
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public LostFoundPublicStatus Status { get; set; } = LostFoundPublicStatus.PendingVerification;
    public LostFoundAdminStatus? AdminStatus { get; set; } = LostFoundAdminStatus.Pending;
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public string? PrimaryImageUrl { get; set; }
    public string? PostedByUserId { get; set; }

    public string? LocationType { get; set; }
    public string? BuildingCode { get; set; }
    public string? IsRoom { get; set; }
    public string? RoomOrArea { get; set; }
    public string? CampusLocation { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? RoomArea { get; set; }
    public string? LastKnownLocation { get; set; }
    public DateOnly? DateLost { get; set; }
    public string? TimeLost { get; set; }
    public string? CollectionPlace { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }

    public string? StorageBinId { get; set; }
    public LostFoundItemCondition? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public string? IntakePhotoUrl { get; set; }
    public bool? ConfirmAccuracy { get; set; }

    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool? StudentIdVerified { get; set; }
    public string? HandoverProofUrl { get; set; }
    public DateTime? DeliveryCompletedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }

    public List<LostFoundItemImage> Images { get; set; } = new();
    public List<LostFoundItemClaim> Claims { get; set; } = new();
}
