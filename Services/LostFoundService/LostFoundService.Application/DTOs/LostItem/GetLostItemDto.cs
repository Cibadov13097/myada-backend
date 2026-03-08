using LostFoundService.Application.DTOs.Location;

namespace LostFoundService.Application.DTOs.LostItem
{
    public class GetLostItemDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? LocationId { get; set; }
        public LocationDto? Location { get; set; }
        public string? LocationLost { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? PostedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
