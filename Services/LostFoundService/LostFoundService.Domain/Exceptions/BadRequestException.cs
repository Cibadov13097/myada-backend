namespace LostFoundService.Domain.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(
        string errorCode,
        string message,
        Dictionary<string, List<string>>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public string ErrorCode { get; }

    public Dictionary<string, List<string>>? Details { get; }
}
