namespace LostFoundService.Application.DTOs.LostItem.Admin
{
    public class AdminLostItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string LocationFound { get; set; }

        public string PostedByUserId { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string? CreatedByUserId { get; set; }
        public string? ModifiedByUserId { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}
