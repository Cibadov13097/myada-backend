using LostFoundService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LostFoundService.Application.DTOs.FoundItem.Admin
{
    public class AdminGetFoundItem
    {
        [JsonPropertyOrder(1)]
        public int Id { get; set; }

        [JsonPropertyOrder(2)]
        public string ImageUrl { get; set; }

        [JsonPropertyOrder(3)]
        public string Description { get; set; }
        [JsonPropertyOrder(4)]
        public string? Location { get; set; }
        [JsonPropertyOrder(5)]
        public int? BuildingId { get; set; }
        [JsonPropertyOrder(6)]
        public int? RoomId { get; set; }
        [JsonPropertyOrder(7)]
        public string FoundByUserId { get; set; }
        [JsonPropertyOrder(8)]
        public Status Status { get; set; } = Status.approved;
        [JsonPropertyOrder(9)]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyOrder(10)]
        public bool IsDeleted { get; set; }
        [JsonPropertyOrder(11)]
        public string CreatedByUserId { get; set; }
        [JsonPropertyOrder(12)]
        public string ModifiedByUserId { get; set; }
    }
}
