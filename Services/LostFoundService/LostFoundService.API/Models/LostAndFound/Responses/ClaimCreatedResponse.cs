namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ClaimCreatedResponse
{
    public int ClaimId { get; set; }
    public string Status { get; set; } = string.Empty;
}
