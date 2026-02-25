using System;

namespace ClubManagementService.Domain.Common
{
    public class AuditableEntity : BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedByUserId { get; set; }

        public DateTime? ModifiedAt { get; set; }
        public Guid? ModifiedByUserId { get; set; }

        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}