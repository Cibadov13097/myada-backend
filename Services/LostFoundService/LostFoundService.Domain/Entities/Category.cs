using LostFoundService.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Domain.Entities
{
    public class Category:AuditableEntity
    {
        public string Name { get; set; }    
    }
}
