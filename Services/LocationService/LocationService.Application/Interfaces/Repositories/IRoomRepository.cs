using LocationService.Domain.Entities;

namespace LocationService.Application.Interfaces.Repositories
{
    public interface IRoomRepository : IGenericRepository<Room>
    {
        Task<List<Room>> GetAllWithBuildingAsync();
        Task<Room?> GetByIdWithBuildingAsync(int id);
        Task<List<Room>> GetByBuildingIdWithBuildingAsync(int buildingId);
    }
}
