namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class ConfirmLostFoundReceiptCommand
{
    public string StorageBinId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? IntakePhotoUrl { get; set; }
    public bool ConfirmAccuracy { get; set; }
    public string? AdminNotes { get; set; }
    public string? ModifiedByUserId { get; set; }
}
