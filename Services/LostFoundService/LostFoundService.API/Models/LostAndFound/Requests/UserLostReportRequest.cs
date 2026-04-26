namespace LostFoundService.Api.Models.LostAndFound.Requests;

public sealed class UserLostReportRequest
{
    public string? Type { get; set; }
    public string? ItemName { get; set; }
    public int? CategoryId { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? DateLost { get; set; }
    public string? TimeLost { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Status { get; set; }
}
