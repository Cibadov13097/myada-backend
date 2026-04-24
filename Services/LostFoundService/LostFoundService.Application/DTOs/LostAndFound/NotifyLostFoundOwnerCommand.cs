namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class NotifyLostFoundOwnerCommand
{
    public string? Channel { get; set; }
    public string? Message { get; set; }
    public string? ModifiedByUserId { get; set; }
}
