using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class SubeventRepository : GenericRepository<Subevent>, ISubeventRepository
{
    private readonly ClubManagementDbContext _context;

    public SubeventRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<Subevent>> GetByMainEventIdAsync(int mainEventId)
    {
        return await _context.Subevents
            .Where(s => s.MainEventId == mainEventId)
            .ToListAsync();
    }
}
