using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities;

public sealed class ClubEmployee : AuditableEntity
{
    public int ClubId { get; set; }
    public Club? Club { get; set; }

    public Guid UserId { get; set; }

    public int? PositionId { get; set; }
}

