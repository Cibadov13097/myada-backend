namespace MyAdaSupportService.Application.DTOs.Request;

public class CreateSupportRequestDto
{
    public string Area { get; set; }              // it | fm
    public int CategoryId { get; set; }
    public string LocationType { get; set; }      // building | campus
    public int? BuildingId { get; set; }
    public string? PlaceType { get; set; }        // room | nonRoom
    public int? RoomId { get; set; }
    public string? AreaDetails { get; set; }      // campusLocation or nonRoomLocation
    public string Description { get; set; }
    public string Urgency { get; set; }           // standard | critical
    public List<string>? AttachmentUrls { get; set; }
}