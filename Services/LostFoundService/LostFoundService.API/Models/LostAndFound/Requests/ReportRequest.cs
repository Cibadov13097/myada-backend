namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class ReportRequest
{
    public string? Type { get; set; }
    public string? ItemName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? Location { get; set; }
    public string? LocationType { get; set; }
    public string? BuildingCode { get; set; }
    public string? IsRoom { get; set; }
    public string? RoomOrArea { get; set; }
    public string? CampusLocation { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? RoomArea { get; set; }
    public string? LastKnownLocation { get; set; }
    public string? DateLost { get; set; }
    public string? TimeLost { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public List<IFormFile>? Files { get; set; }
    public IFormFile? IntakePhotoFile { get; set; }
    public IFormFile? HandoverProofFile { get; set; }
    public string? CollectionPlace { get; set; }
    public string? Status { get; set; }
    public string? AdminStatus { get; set; }
    public string? StorageBinId { get; set; }
    public string? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public bool? ConfirmAccuracy { get; set; }
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool? StudentIdVerified { get; set; }
}
