namespace LostFoundService.Api.Models.LostAndFound.Responses;

public sealed class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, List<string>>? Details { get; set; }
}
