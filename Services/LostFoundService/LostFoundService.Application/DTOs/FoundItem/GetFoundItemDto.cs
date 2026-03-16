using LostFoundService.Domain.Enums;
using System.Text.Json.Serialization;

namespace LostFoundService.Application.DTOs.FoundItem
{
    public class GetFoundItemDto
    {
        [JsonPropertyOrder(1)]
        public int Id { get; set; }

        [JsonPropertyOrder(2)]
        public string? ImageUrl { get; set; }

        [JsonPropertyOrder(3)]
        public string? Description { get; set; }
        [JsonPropertyOrder(4)]
        public int? BuildingId { get; set; }
        [JsonPropertyOrder(5)]
        public int? RoomId { get; set; }
        [JsonPropertyOrder(6)]
        public string? FoundByUserId { get; set; }
        [JsonPropertyOrder(7)]
        public Status Status { get; set; }
        [JsonPropertyOrder(8)]
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        [JsonPropertyOrder(9)]
        public DateTime CreatedAt { get; set; }
    }
}
