namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class NotifyRequest
{
    public string? Channel { get; set; }
    public string? Message { get; set; }
}
