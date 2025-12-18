using LostFoundService.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Domain.Entities
{
    public class LostItem:AuditableEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }


        public string LocationLost { get; set; }
        public string PostedByUserId { get; set; }


        public Category Category { get; set; }
        public int CategoryId { get; set; }

    }
}
