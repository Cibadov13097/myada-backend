using ClubManagementService.Application.DTOs.EventRegistration;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IEventRegistrationService
{
    Task<EventRegistrationDto?> GetByIdAsync(int id);
    Task<List<EventRegistrationDto>> GetAllAsync();
    Task<List<EventRegistrationDto>> GetByEventIdAsync(Guid eventId);
    Task<EventRegistrationDto> CreateAsync(CreateEventRegistrationDto dto);
    Task DeleteAsync(int id);
}
