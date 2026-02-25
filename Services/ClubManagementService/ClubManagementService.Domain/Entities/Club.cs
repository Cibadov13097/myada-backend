using System;
using ClubManagementService.Domain.Common;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Domain.Entities
{
    public class Club : AuditableEntity
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid ApprovedBy { get; set; }
        public ClubStatus Status { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
