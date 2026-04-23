using LostFoundService.Domain.Common;
using LostFoundService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Domain.Entities
{
  
    public class FoundItem : AuditableEntity
    {
        public string Description { get; set; }
        public string? Location { get; set; }
        public int? BuildingId { get; set; }
        public int? RoomId { get; set; }
        public string FoundByUserId { get; set; }
         public string ImageUrl { get; set; }
        public Status Status { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
    

}
