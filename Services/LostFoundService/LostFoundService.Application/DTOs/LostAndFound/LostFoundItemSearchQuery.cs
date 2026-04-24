namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class LostFoundItemSearchQuery
{
    public string? Q { get; set; }
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? PostedBy { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? AdminStatus { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? CurrentUserId { get; set; }
    public bool IncludeAdminFields { get; set; }
}
