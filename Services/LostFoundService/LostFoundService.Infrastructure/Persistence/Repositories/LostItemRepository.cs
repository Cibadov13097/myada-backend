using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Entities;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Repositories
{
    public class LostItemRepository : GenericRepository<LostItem>, ILostItemRepository
    {
        private readonly LostFoundDbContext _context;

        public LostItemRepository(LostFoundDbContext context)
            : base(context)
        {
            _context = context;
        }

        // ============================
        // USER-SCOPED
        // ============================

        public async Task<List<LostItem>> GetAllByUserIdAsync(string userId)
        {
            return await _context.LostItems
                .Where(x => x.PostedByUserId == userId)
                .ToListAsync();
        }

        public async Task<LostItem?> GetByIdForUserAsync(int id, string userId)
        {
            return await _context.LostItems
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.PostedByUserId == userId);
        }

        // ============================
        // ADMIN
        // ============================

        public async Task<LostItem?> GetByIdForAdminAsync(int id)
        {
            return await _context.LostItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<LostItem>> GetAllForAdminAsync(bool includeDeleted = true)
        {
            if (includeDeleted)
            {
                return await _context.LostItems
                    .IgnoreQueryFilters()
                    .ToListAsync();
            }

            return await _context.LostItems.ToListAsync();
        }
    }
}
