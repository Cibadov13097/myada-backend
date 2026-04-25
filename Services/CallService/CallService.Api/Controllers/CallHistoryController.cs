using System.Security.Claims;
using CallService.Api.Persistence;
using CallService.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/call-history")]
[Route("api/calls/history")]
public sealed class CallHistoryController(ICallHistoryService callHistoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CallHistoryResponse>>> GetHistory(
        [FromQuery] string? status,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        var normalizedLimit = Math.Clamp(limit, 1, 200);

        if (!TryParseStatus(status, out var parsedStatus))
            return BadRequest("Invalid status. Use pending, accepted, rejected, cancelled, or timed-out.");

        var history = await callHistoryService.GetHistoryForUserAsync(
            currentUserId,
            parsedStatus,
            normalizedLimit,
            cancellationToken);

        return Ok(history.Select(MapResponse).ToArray());
    }

    [HttpGet("{callId}")]
    public async Task<ActionResult<CallHistoryResponse>> GetByCallId(
        string callId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callId))
            return BadRequest("callId is required.");

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        var call = await callHistoryService.GetCallForUserAsync(callId.Trim(), currentUserId, cancellationToken);

        if (call is null)
            return NotFound();

        return Ok(MapResponse(call));
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrWhiteSpace(userId) ? string.Empty : userId;
    }

    private static bool TryParseStatus(string? rawStatus, out CallRequestStatus? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(rawStatus))
            return true;

        var normalized = rawStatus.Trim().Replace("-", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<CallRequestStatus>(normalized, true, out var parsed))
            return false;

        status = parsed;
        return true;
    }

    private static CallHistoryResponse MapResponse(CallHistoryRecord record)
    {
        return new CallHistoryResponse(
            record.CallId,
            record.RoomId,
            ToClientStatus(record.Status),
            new CallPartyResponse(record.CallerUserId, record.CallerDisplayName),
            new CallPartyResponse(record.DispatcherUserId, record.DispatcherDisplayName),
            record.RequestedAtUtc,
            record.AcceptedAtUtc,
            record.EndedAtUtc,
            record.DurationSeconds,
            record.ResolvedAtUtc,
            record.ResolveReason,
            record.EndReason);
    }

    private static string ToClientStatus(CallRequestStatus status)
    {
        return status switch
        {
            CallRequestStatus.Pending => "pending",
            CallRequestStatus.Accepted => "accepted",
            CallRequestStatus.Rejected => "rejected",
            CallRequestStatus.Cancelled => "cancelled",
            CallRequestStatus.TimedOut => "timed-out",
            _ => "unknown"
        };
    }

    public sealed record CallHistoryResponse(
        string CallId,
        string RoomId,
        string Status,
        CallPartyResponse Caller,
        CallPartyResponse Dispatcher,
        DateTime RequestedAtUtc,
        DateTime? AcceptedAtUtc,
        DateTime? EndedAtUtc,
        long? DurationSeconds,
        DateTime? ResolvedAtUtc,
        string? ResolveReason,
        string? EndReason);

    public sealed record CallPartyResponse(string UserId, string DisplayName);
}
