using LocationService.Application.DTOs;

namespace LocationService.Application.Interfaces.Services
{
    public interface IRoomService
    {
        Task<RoomDto?> GetByIdAsync(int id);
        Task<List<RoomDto>> GetAllAsync();
        Task<List<RoomDto>> GetByBuildingIdAsync(int buildingId);
        Task<RoomDto> CreateAsync(CreateRoomDto dto);
        Task<RoomDto> UpdateAsync(int id, UpdateRoomDto dto);
        Task DeleteAsync(int id);
    }
}

