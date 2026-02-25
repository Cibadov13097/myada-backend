using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class JobVacancy : AuditableEntity
    {
        public int ClubId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid PostedBy { get; set; }
    }
}
