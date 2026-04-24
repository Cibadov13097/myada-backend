namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ReportCreatedResponse
{
    public int Id { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
