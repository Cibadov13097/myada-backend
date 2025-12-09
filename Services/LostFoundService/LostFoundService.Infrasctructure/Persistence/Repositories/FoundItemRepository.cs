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
                .Where(x => x.FoundByUserId == userId)
                .ToListAsync();
        }

        public async Task<FoundItem> GetByIdForUserAsync(int id, string userId)
        {
            return await _context.FoundItems
                .FirstOrDefaultAsync(x => x.Id == id && x.FoundByUserId == userId);
        }
    }
}
