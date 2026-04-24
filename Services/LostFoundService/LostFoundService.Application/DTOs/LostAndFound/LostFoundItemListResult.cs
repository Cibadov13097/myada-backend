using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class LostFoundItemListResult
{
    public List<LostFoundItem> Items { get; set; } = new();
    public int Total { get; set; }
}
