namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class ConfirmReceiptRequest
{
    public string StorageBinId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public IFormFile? IntakePhotoFile { get; set; }
    public bool ConfirmAccuracy { get; set; }
    public string? AdminNotes { get; set; }
}
