using LocationService.Application.Interfaces.Repositories;
using LocationService.Domain.Entities;
using LocationService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LocationService.Infrastructure.Persistence.Repositories
{
    public class RoomRepository : GenericRepository<Room>, IRoomRepository
    {
        public RoomRepository(LocationDbContext locationDbContext) : base(locationDbContext)
        {
        }

        public async Task<List<Room>> GetAllWithBuildingAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Building)
                .ToListAsync();
        }

        public async Task<Room?> GetByIdWithBuildingAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Building)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Room>> GetByBuildingIdWithBuildingAsync(int buildingId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Building)
                .Where(x => x.BuildingId == buildingId)
                .ToListAsync();
        }
    }
}
