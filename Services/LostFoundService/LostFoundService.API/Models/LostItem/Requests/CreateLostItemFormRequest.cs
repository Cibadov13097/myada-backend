namespace LostFoundService.Api.Models.LostItem.Requests;

public sealed class CreateLostItemFormRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public int CategoryId { get; set; }
    public IFormFile? ImageFile { get; set; }
}
