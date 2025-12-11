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

        // Custom: Get all items for this user
        public async Task<List<FoundItem>> GetAllByUserIdAsync(string userId)
        {
            return await _context.FoundItems
                .Where(x => x.FoundByUserId == userId && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<FoundItem> GetByIdForUserAsync(int id, string userId)
        {
            return await _context.FoundItems
                .FirstOrDefaultAsync(x => x.Id == id && x.FoundByUserId == userId && !x.IsDeleted);
        }


        public async Task<FoundItem> GetByIdForAdminAsync(int id)
        {
            var query = _context.FoundItems.AsQueryable();

            

            return await query.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<FoundItem>> GetAllForAdminAsync( bool includeDeleted = true)
        {
            var query = _context.FoundItems.AsQueryable();

            if (!includeDeleted)
                query = query.Where(x => !x.IsDeleted);

            return await query.ToListAsync() ;
        }
    }
}
