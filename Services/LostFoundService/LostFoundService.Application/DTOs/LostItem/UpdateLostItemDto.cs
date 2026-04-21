using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.LostItem
{
    public class UpdateLostItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? BuildingId { get; set; }
        public int? RoomId { get; set; }
        [JsonIgnore]
        public string? Image { get; set; }
        public int? CategoryId { get; set; }
    }
}
