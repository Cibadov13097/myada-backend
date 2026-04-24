using LostFoundService.Domain.Enums;

namespace LostFoundService.Api.Models.FoundItem.Requests;

public sealed class UpdateFoundItemFormRequest
{
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public Status Status { get; set; }
    public int? CategoryId { get; set; }
    public IFormFile? ImageFile { get; set; }
}
