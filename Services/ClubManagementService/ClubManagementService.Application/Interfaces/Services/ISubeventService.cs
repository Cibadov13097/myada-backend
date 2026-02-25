using ClubManagementService.Application.DTOs.Subevent;

namespace ClubManagementService.Application.Interfaces.Services;

public interface ISubeventService
{
    Task<SubeventDto?> GetByIdAsync(int id);
    Task<List<SubeventDto>> GetAllAsync();
    Task<List<SubeventDto>> GetByMainEventIdAsync(int mainEventId);
    Task<SubeventDto> CreateAsync(CreateSubeventDto dto);
    Task<SubeventDto> UpdateAsync(int id, UpdateSubeventDto dto);
    Task DeleteAsync(int id);
}
