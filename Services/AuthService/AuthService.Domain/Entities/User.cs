using AuthService.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Entities
{
    public class User:AuditableEntity
    {   
        public Guid Id { get; private set; }

        public string UserName { get; set; }
        public string FirstName { get; private set; }
        public string LastName { get; set; }
        public string Email { get; private set; }
        public string OrganizationalId { get; set; } = null!;
        public string? ProfileImage { get; set; }

    }

}
