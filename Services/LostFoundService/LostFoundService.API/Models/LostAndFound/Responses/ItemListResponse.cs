namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ItemListResponse
{
    public List<LostFoundItemResponse> Items { get; set; } = new();
    public int Total { get; set; }
}
