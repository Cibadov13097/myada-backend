using CallService.Api.Realtime;
using Microsoft.EntityFrameworkCore;

namespace CallService.Api.Persistence;

public sealed class CallHistoryService(IDbContextFactory<CallHistoryDbContext> dbContextFactory) : ICallHistoryService
{
    public async Task CreatePendingCallAsync(
        CallRequestState pendingCall,
        string dispatcherDisplayName,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await dbContext.CallHistory
            .SingleOrDefaultAsync(x => x.CallId == pendingCall.CallId, cancellationToken);

        if (existing is not null)
        {
            ApplyCallState(existing, pendingCall, dispatcherDisplayName);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var record = new CallHistoryRecord
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyCallState(record, pendingCall, dispatcherDisplayName);
        dbContext.CallHistory.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCallStatusAsync(
        CallRequestState call,
        string dispatcherDisplayName,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var record = await dbContext.CallHistory
            .SingleOrDefaultAsync(x => x.CallId == call.CallId, cancellationToken);

        if (record is null)
        {
            record = new CallHistoryRecord
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.CallHistory.Add(record);
        }

        ApplyCallState(record, call, dispatcherDisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CallHistoryRecord?> TryCompleteAcceptedCallAsync(
        string roomId,
        string endedByUserId,
        string? endedByConnectionId,
        DateTime endedAtUtc,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var record = await dbContext.CallHistory
            .SingleOrDefaultAsync(x =>
                x.RoomId == roomId &&
                x.Status == CallRequestStatus.Accepted &&
                x.EndedAtUtc == null,
                cancellationToken);

        if (record is null)
            return null;

        record.EndedAtUtc = endedAtUtc;
        record.EndedByUserId = endedByUserId;
        record.EndedByConnectionId = endedByConnectionId;
        record.EndReason = NormalizeReason(reason);
        record.UpdatedAtUtc = DateTime.UtcNow;

        if (record.AcceptedAtUtc.HasValue)
        {
            var duration = endedAtUtc - record.AcceptedAtUtc.Value;
            record.DurationSeconds = Math.Max(0, (long)Math.Floor(duration.TotalSeconds));
        }
        else
        {
            record.DurationSeconds = 0;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<CallHistoryRecord>> GetHistoryForUserAsync(
        string userId,
        CallRequestStatus? status,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.CallHistory
            .AsNoTracking()
            .Where(x => x.CallerUserId == userId || x.DispatcherUserId == userId);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<CallHistoryRecord?> GetCallForUserAsync(
        string callId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.CallHistory
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.CallId == callId &&
                (x.CallerUserId == userId || x.DispatcherUserId == userId),
                cancellationToken);
    }

    private static void ApplyCallState(
        CallHistoryRecord record,
        CallRequestState call,
        string dispatcherDisplayName)
    {
        record.CallId = call.CallId;
        record.RoomId = call.RoomId;
        record.CallerUserId = call.CallerUserId;
        record.CallerDisplayName = call.CallerDisplayName;
        record.DispatcherUserId = call.DispatcherUserId;
        record.DispatcherDisplayName = string.IsNullOrWhiteSpace(dispatcherDisplayName)
            ? record.DispatcherDisplayName
            : dispatcherDisplayName.Trim();
        record.CallerConnectionId = call.CallerConnectionId;
        record.DispatcherConnectionId = call.ResolvedByConnectionId ?? record.DispatcherConnectionId;
        record.Status = call.Status;
        record.RequestedAtUtc = call.RequestedAtUtc;
        record.ExpiresAtUtc = call.ExpiresAtUtc;
        record.ResolvedAtUtc = call.ResolvedAtUtc;
        record.ResolvedByUserId = call.ResolvedByUserId;
        record.ResolvedByConnectionId = call.ResolvedByConnectionId;
        record.ResolveReason = NormalizeReason(call.ResolveReason);
        record.UpdatedAtUtc = DateTime.UtcNow;

        if (call.Status == CallRequestStatus.Accepted)
        {
            record.AcceptedAtUtc ??= call.ResolvedAtUtc ?? DateTime.UtcNow;
            record.DispatcherConnectionId ??= call.ResolvedByConnectionId;
        }
    }

    private static string? NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
