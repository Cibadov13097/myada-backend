using ClubManagementService.Application.DTOs.EventRegistration;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IEventRegistrationService
{
    Task<EventRegistrationDto?> GetByIdAsync(int id);
    Task<List<EventRegistrationDto>> GetAllAsync();
    Task<List<EventRegistrationDto>> GetByEventIdAsync(int eventId);
    Task<EventRegistrationDto?> GetByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<EventRegistrationDto> CreateAsync(CreateEventRegistrationDto dto);
    Task DeleteAsync(int id);
    Task DeleteByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);

    Task<EventRegistrationDto> RegisterAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<EventRegistrationDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EventRegistrationDto?> GetTicketAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInResult> CheckInAsync(int eventId, string jwt, Guid staffUserId, CancellationToken cancellationToken = default);
}

public sealed record CheckInResult(
    bool Success,
    string Status,
    string? Message,
    string TicketId,
    Guid AttendeeId,
    int EventId,
    DateTime? CheckedInAt);
