using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Entities;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Repositories
{
    public class LostItemRepository : GenericRepository<LostItem>, ILostItemRepository
    {
        private readonly LostFoundDbContext _context;

        private static IQueryable<LostItem> WithIncludes(IQueryable<LostItem> q) =>
            q.Include(x => x.Category);

        public LostItemRepository(LostFoundDbContext context)
            : base(context)
        {
            _context = context;
        }

        public override async Task<List<LostItem>> GetAllAsync()
        {
            return await WithIncludes(_context.LostItems).ToListAsync();
        }

        public override async Task<LostItem?> GetByIdAsync(int id)
        {
            return await WithIncludes(_context.LostItems).FirstOrDefaultAsync(x => x.Id == id);
        }

        // ============================
        // USER-SCOPED
        // ============================

        public async Task<List<LostItem>> GetAllByUserIdAsync(string userId)
        {
            return await WithIncludes(_context.LostItems)
                .Where(x => x.PostedByUserId == userId)
                .ToListAsync();
        }

        public async Task<LostItem?> GetByIdForUserAsync(int id, string userId)
        {
            return await WithIncludes(_context.LostItems)
                .FirstOrDefaultAsync(x => x.Id == id && x.PostedByUserId == userId);
        }

        // ============================
        // ADMIN
        // ============================

        public async Task<LostItem?> GetByIdForAdminAsync(int id)
        {
            return await WithIncludes(_context.LostItems)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<LostItem>> GetAllForAdminAsync(bool includeDeleted = true)
        {
            var query = WithIncludes(_context.LostItems);
            if (includeDeleted)
                query = query.IgnoreQueryFilters();
            return await query.ToListAsync();
        }
    }
}
