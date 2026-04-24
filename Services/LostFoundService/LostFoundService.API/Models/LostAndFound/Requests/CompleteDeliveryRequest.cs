namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class CompleteDeliveryRequest
{
    public string? ClaimantName { get; set; }
    public string? ClaimantStudentId { get; set; }
    public bool StudentIdVerified { get; set; }
    public IFormFile? HandoverProofFile { get; set; }
}
