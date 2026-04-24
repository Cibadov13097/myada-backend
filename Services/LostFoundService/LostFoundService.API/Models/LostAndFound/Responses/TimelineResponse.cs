namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class TimelineResponse
{
    public List<TimelineEventResponse> Events { get; set; } = new();
}
