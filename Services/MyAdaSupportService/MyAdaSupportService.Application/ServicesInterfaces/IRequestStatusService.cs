namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface IRequestStatusService
{
    Task MarkAsStartedAsync(int requestId, Guid staffId);
    Task MarkAsInProgressAsync(int requestId, Guid staffId);
    Task MarkAsCompletedAsync(int requestId, Guid staffId);
}
