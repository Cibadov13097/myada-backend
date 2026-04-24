namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ItemClaimResponse
{
    public int Id { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
