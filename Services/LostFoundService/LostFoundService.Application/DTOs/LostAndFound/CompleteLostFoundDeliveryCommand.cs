namespace LostFoundService.Application.DTOs.LostAndFound;

public sealed class CompleteLostFoundDeliveryCommand
{
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool StudentIdVerified { get; set; }
    public string? HandoverProofUrl { get; set; }
    public string? ModifiedByUserId { get; set; }
}
