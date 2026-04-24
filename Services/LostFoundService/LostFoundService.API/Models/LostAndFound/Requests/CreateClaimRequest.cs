namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class CreateClaimRequest
{
    public string? ClaimType { get; set; }
    public string? Message { get; set; }
}
