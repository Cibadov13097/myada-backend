using LostFoundService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem
{
    public class UpdateFoundItemDto
    {
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string LocationFound { get; set; }
        public Status Status { get; set; }
        public int CategoryId { get; set; }
    }
}
