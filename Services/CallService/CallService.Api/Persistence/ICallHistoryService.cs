using CallService.Api.Realtime;

namespace CallService.Api.Persistence;

public interface ICallHistoryService
{
    Task CreatePendingCallAsync(
        CallRequestState pendingCall,
        string dispatcherDisplayName,
        CancellationToken cancellationToken = default);

    Task UpdateCallStatusAsync(
        CallRequestState call,
        string dispatcherDisplayName,
        CancellationToken cancellationToken = default);

    Task<CallHistoryRecord?> TryCompleteAcceptedCallAsync(
        string roomId,
        string endedByUserId,
        string? endedByConnectionId,
        DateTime endedAtUtc,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CallHistoryRecord>> GetHistoryForUserAsync(
        string userId,
        CallRequestStatus? status,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CallHistoryRecord?> GetCallForUserAsync(
        string callId,
        string userId,
        CancellationToken cancellationToken = default);
}
