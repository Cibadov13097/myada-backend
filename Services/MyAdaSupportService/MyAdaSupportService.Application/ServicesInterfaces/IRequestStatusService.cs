namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface IRequestStatusService
{
    Task MarkAsStartedAsync(int requestId, int staffId);
    Task MarkAsInProgressAsync(int requestId, int staffId);
    Task MarkAsCompletedAsync(int requestId, int staffId);
}
