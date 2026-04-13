namespace MyAdaSupportService.Application.Abstractions;

public interface ISupportRealtimeNotifier
{
    Task NotifySupportRequestChangedAsync(int requestId, string eventType);
}
