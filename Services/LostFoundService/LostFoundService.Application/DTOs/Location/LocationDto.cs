namespace LostFoundService.Application.DTOs.Location
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string Place { get; set; } = string.Empty;
        public string? Floor { get; set; }
        public string? Room { get; set; }
        public string? ImageUrl { get; set; }
    }
}
