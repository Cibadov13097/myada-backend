using System;

namespace LocationService.Domain.Common
{
    public class AuditableEntity : BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }

        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedByUserId { get; set; }

        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}