using MyAdaSupportService.Domain.Entities.BaseAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Domain.Entities.RequestAggregate;

public class SupportStaffStatus : BaseAuditedEntity
{
    public int MemberId { get; set; }
    public StaffAvailabilityStatus Status { get; set; }
}
