namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ItemClaimsResponse
{
    public List<ItemClaimResponse> Claims { get; set; } = new();
}
