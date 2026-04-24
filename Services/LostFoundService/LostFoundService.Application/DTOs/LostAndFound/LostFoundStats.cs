namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class LostFoundStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int PendingVerification { get; set; }
}
