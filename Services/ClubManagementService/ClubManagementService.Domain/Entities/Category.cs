using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class Category : AuditableEntity
    {
        public string Name { get; set; } = null!;
    }
}

