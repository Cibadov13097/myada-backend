namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class StatsResponse
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int PendingVerification { get; set; }
}
