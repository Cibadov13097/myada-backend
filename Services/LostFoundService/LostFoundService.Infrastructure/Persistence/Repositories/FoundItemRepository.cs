using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Entities;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Repositories
{
    public class FoundItemRepository
        : GenericRepository<FoundItem>, IFoundItemRepository
    {
        private readonly LostFoundDbContext _context;

        public FoundItemRepository(LostFoundDbContext context)
            : base(context)
        {
            _context = context;
        }

        // ============================
        // USER-SCOPED
        // ============================

        public async Task<List<FoundItem>> GetAllByUserIdAsync(string userId)
        {
            return await _context.FoundItems
                .Where(x => x.FoundByUserId == userId)
                .ToListAsync();
        }

        public async Task<FoundItem?> GetByIdForUserAsync(int id, string userId)
        {
            return await _context.FoundItems
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.FoundByUserId == userId);
        }

        // ============================
        // ADMIN
        // ============================

        public async Task<FoundItem?> GetByIdForAdminAsync(int id)
        {
            return await _context.FoundItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<FoundItem>> GetAllForAdminAsync(bool includeDeleted = true)
        {
            if (includeDeleted)
            {
                return await _context.FoundItems
                    .IgnoreQueryFilters()
                    .ToListAsync();
            }

            // includeDeleted == false → global filter işləyir
            return await _context.FoundItems.ToListAsync();
        }
    }
}
