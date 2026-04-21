using ClubManagementService.Application.DTOs.EventRegistration;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IEventRegistrationService
{
    Task<EventRegistrationDto?> GetByIdAsync(int id);
    Task<List<EventRegistrationDto>> GetAllAsync();
    Task<List<EventRegistrationDto>> GetByEventIdAsync(int eventId);
    Task<EventRegistrationDto?> GetByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<EventRegistrationDto> CreateAsync(CreateEventRegistrationDto dto);
    Task<EventRegistrationDto> UpdateAsync(int id, UpdateEventRegistrationDto dto);
    Task DeleteAsync(int id);
    Task DeleteByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
}
