using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities;

public class ClubMembership : AuditableEntity
{
    public int ClubId { get; set; }
    public Club? Club { get; set; }
    public Guid UserId { get; set; }
}
