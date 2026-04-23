using ClubManagementService.Application.DTOs.Interview;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IInterviewSchedulingService
{
    Task<InterviewSlotListResult> ListSlotsAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateOnly? onDate,
        bool includeFull,
        CancellationToken cancellationToken = default);

    Task<InterviewBookingResultDto> BookSlotAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        string slotId,
        CancellationToken cancellationToken = default);

    Task<InterviewBookingResultDto> GetBookingAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
