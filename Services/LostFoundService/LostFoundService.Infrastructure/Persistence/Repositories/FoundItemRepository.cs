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

        private static IQueryable<FoundItem> WithIncludes(IQueryable<FoundItem> q) =>
            q.Include(x => x.Category);

        public FoundItemRepository(LostFoundDbContext context)
            : base(context)
        {
            _context = context;
        }

        public override async Task<List<FoundItem>> GetAllAsync()
        {
            return await WithIncludes(_context.FoundItems).ToListAsync();
        }

        public override async Task<FoundItem?> GetByIdAsync(int id)
        {
            return await WithIncludes(_context.FoundItems).FirstOrDefaultAsync(x => x.Id == id);
        }

        // ============================
        // USER-SCOPED
        // ============================

        public async Task<List<FoundItem>> GetAllByUserIdAsync(string userId)
        {
            return await WithIncludes(_context.FoundItems)
                .Where(x => x.FoundByUserId == userId)
                .ToListAsync();
        }

        public async Task<FoundItem?> GetByIdForUserAsync(int id, string userId)
        {
            return await WithIncludes(_context.FoundItems)
                .FirstOrDefaultAsync(x => x.Id == id && x.FoundByUserId == userId);
        }

        // ============================
        // ADMIN
        // ============================

        public async Task<FoundItem?> GetByIdForAdminAsync(int id)
        {
            return await WithIncludes(_context.FoundItems)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<FoundItem>> GetAllForAdminAsync(bool includeDeleted = true)
        {
            var query = WithIncludes(_context.FoundItems);
            if (includeDeleted)
                query = query.IgnoreQueryFilters();
            return await query.ToListAsync();
        }
    }
}
