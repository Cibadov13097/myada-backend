using ClubManagementService.Domain.Common;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Domain.Entities;

/// <summary>Reusable club role template (title, category, requirements) used when creating vacancies.</summary>
public class ClubPosition : AuditableEntity
{
    public int ClubId { get; set; }
    public Club? Club { get; set; }
    public string Title { get; set; } = null!;
    public ClubPositionCategory Category { get; set; }
    public ICollection<ClubPositionRequirement> Requirements { get; set; } = new List<ClubPositionRequirement>();
}
