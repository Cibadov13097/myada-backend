namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class LostFoundItemResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminStatus { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string DatePosted { get; set; } = string.Empty;
    public int DaysAgo { get; set; }
    public DateTime PostedAt { get; set; }
    public string? Image { get; set; }
    public List<string> Images { get; set; } = new();
    public string? PostedByUserId { get; set; }
    public string? CollectionPlace { get; set; }
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
    public string? StorageBinId { get; set; }
    public string? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public string? IntakePhotoUrl { get; set; }
    public bool? ConfirmAccuracy { get; set; }
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool? StudentIdVerified { get; set; }
    public string? HandoverProofUrl { get; set; }
}
