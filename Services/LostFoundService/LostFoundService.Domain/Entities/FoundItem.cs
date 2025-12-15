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

            public string ImageUrl { get; set; }
            public string Description { get; set; }
            public string LocationFound { get; set; }      
            public string FoundByUserId { get; set; }
            public Status Status { get; set; }
        }
    

}
