namespace LostFoundService.Domain.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
