using Microsoft.AspNetCore.SignalR;
using MyAdaSupportService.Api.Hubs;
using MyAdaSupportService.Application.Abstractions;

namespace MyAdaSupportService.Api.Realtime;

public class SupportRealtimeNotifier : ISupportRealtimeNotifier
{
    private readonly IHubContext<SupportUpdatesHub> _hubContext;

    public SupportRealtimeNotifier(IHubContext<SupportUpdatesHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySupportRequestChangedAsync(int requestId, string eventType)
    {
        await _hubContext.Clients.All.SendAsync("supportRequestChanged", new
        {
            requestId,
            eventType,
            changedAt = DateTime.UtcNow
        });
    }
}
