using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem.Admin
{
    public class AdminCreateFoundItem
    {
        public string ImageUrl { get; set; }

        
        public string Description { get; set; }
        public int? BuildingId { get; set; }
        public int? RoomId { get; set; }
        public int CategoryId { get; set; }
    }
}
