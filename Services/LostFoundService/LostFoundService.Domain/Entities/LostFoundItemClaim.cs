using LostFoundService.Domain.Common;
using LostFoundService.Domain.Enums;

namespace LostFoundService.Domain.Entities;

public class LostFoundItemClaim : AuditableEntity
{
    public int LostFoundItemId { get; set; }
    public LostFoundItem LostFoundItem { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public LostFoundClaimType ClaimType { get; set; }
    public string? Message { get; set; }
    public LostFoundClaimStatus Status { get; set; } = LostFoundClaimStatus.PendingReview;
}
