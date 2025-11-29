using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Common
{
    public class AuditableEntity : BaseEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }

        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByUserId { get; set; }

        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

}
