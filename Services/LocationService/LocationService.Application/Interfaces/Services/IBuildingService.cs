using LocationService.Application.DTOs;

namespace LocationService.Application.Interfaces.Services
{
    public interface IBuildingService
    {
        Task<BuildingDto?> GetByIdAsync(int id);
        Task<List<BuildingDto>> GetAllAsync();
        Task<BuildingDto> CreateAsync(CreateBuildingDto dto);
        Task<BuildingDto> UpdateAsync(int id, UpdateBuildingDto dto);
        Task DeleteAsync(int id);
    }
}

