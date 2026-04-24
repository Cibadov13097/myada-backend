namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class CreateLostFoundClaimCommand
{
    public string? ClaimType { get; set; }
    public string? Message { get; set; }
    public string UserId { get; set; } = string.Empty;
}
