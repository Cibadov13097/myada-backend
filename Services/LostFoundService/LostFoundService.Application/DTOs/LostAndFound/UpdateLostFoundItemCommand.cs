namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class UpdateLostFoundItemCommand
{
    public string? AdminStatus { get; set; }
    public int? CategoryId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? StorageBinId { get; set; }
    public string? Condition { get; set; }
    public string? AdminNotes { get; set; }
    public string? ReportedColor { get; set; }
    public string? ModifiedByUserId { get; set; }
}
