using ClubManagementService.Application.DTOs.Event;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IEventService
{
    Task<EventDto?> GetByIdAsync(int id);
    Task<List<EventDto>> GetAllAsync();
    Task<EventDto> CreateAsync(CreateEventDto dto);
    Task<EventDto> UpdateAsync(int id, UpdateEventDto dto);
    Task DeleteAsync(int id);
}
