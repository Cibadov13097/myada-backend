using LostFoundService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem
{
    public class UpdateFoundItemDto
    {
        [JsonIgnore]
        public string? ImageUrl { get; set; }
        public string Description { get; set; }
        public string? Location { get; set; }
        public int? BuildingId { get; set; }
        public int? RoomId { get; set; }
        public Status Status { get; set; }
        public int? CategoryId { get; set; }
    }
}
