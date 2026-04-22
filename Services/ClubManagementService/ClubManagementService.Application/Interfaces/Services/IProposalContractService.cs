using ClubManagementService.Application.DTOs.Contracts;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IProposalContractService
{
    Task<ClubProposalSubmitResult> SubmitClubProposalAsync(string? userId, ClubProposalSubmissionDto submission, CancellationToken cancellationToken = default);

    Task<object> ListClubProposalsAsync(string? status, string? search, int page, int limit, CancellationToken cancellationToken = default);

    Task<object?> GetClubProposalAsync(string proposalIdOrInternalId, CancellationToken cancellationToken = default);

    Task<object?> ApproveClubProposalAsync(string proposalIdOrInternalId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<object?> RejectClubProposalAsync(string proposalIdOrInternalId, string reason, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<object?> RequestClubProposalRevisionAsync(string proposalIdOrInternalId, string changes, Guid actorUserId, CancellationToken cancellationToken = default);

    ClubProposalRequirementsDto GetClubProposalRequirements();

    ClubProposalRequirementsDto SetClubProposalRequirements(ClubProposalRequirementsDto dto);

    Task<EventProposalSubmitResult> SubmitStaffEventProposalAsync(EventProposalSubmitDto body, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<EventProposalSubmitResult> SubmitClubAdminEventProposalAsync(string clubRouteKey, EventProposalSubmitDto body, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<object> ListEventProposalsAsync(string? status, string? search, int page, int limit, CancellationToken cancellationToken = default);

    Task<object?> GetEventProposalAsync(string proposalIdOrInternalId, CancellationToken cancellationToken = default);

    Task<object?> ApproveEventProposalAsync(string proposalIdOrInternalId, EventProposalApproveDto body, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<object?> RejectEventProposalAsync(string proposalIdOrInternalId, string reason, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<object?> RequestEventProposalRevisionAsync(string proposalIdOrInternalId, string changes, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class ClubProposalSubmitResult
{
    public bool Success { get; init; }
    public object? Payload { get; init; }
    public object? ErrorBody { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class EventProposalSubmitResult
{
    public bool Success { get; init; }
    public object? Payload { get; init; }
    public object? ErrorBody { get; init; }
    public string? ErrorMessage { get; init; }
}
