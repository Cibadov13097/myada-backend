using LostFoundService.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Domain.Entities
{
    public class Location : AuditableEntity
    {
        public string Place { get; set; }
        public string? Floor { get; set; }
        public string? Room { get; set; }
    }
}
