namespace LostFoundService.Application.DTOs.LostItem
{
    public class GetLostItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string LocationLost { get; set; }

        public string PostedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
