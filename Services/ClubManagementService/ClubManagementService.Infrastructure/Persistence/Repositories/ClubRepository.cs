using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class ClubRepository : GenericRepository<Club>, IClubRepository
{
    private readonly ClubManagementDbContext _context;

    public ClubRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<Club?> GetByIdWithCategoryAsync(int id)
    {
        return await _context.Clubs
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Club>> GetByCategoryIdAsync(int categoryId)
    {
        return await _context.Clubs
            .Where(c => c.CategoryId == categoryId)
            .Include(c => c.Category)
            .ToListAsync();
    }
}
