using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {

        
        public string FirstName { get;  set; }
        public string LastName { get; set; }
        public string OrganizationalId { get; set; } = null!;
        public string? ProfileImage { get; set; }
        public UserType UserType { get; set; }

        public Status Status { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }

        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByUserId { get; set; }

        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

}
