using LostFoundService.Domain.Common;

namespace LostFoundService.Domain.Entities;

public class LostFoundItemImage : BaseEntity
{
    public int LostFoundItemId { get; set; }
    public LostFoundItem LostFoundItem { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
