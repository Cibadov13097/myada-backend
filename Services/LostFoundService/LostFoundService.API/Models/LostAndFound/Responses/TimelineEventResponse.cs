namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class TimelineEventResponse
{
    public DateTime? At { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool Done { get; set; }
}
