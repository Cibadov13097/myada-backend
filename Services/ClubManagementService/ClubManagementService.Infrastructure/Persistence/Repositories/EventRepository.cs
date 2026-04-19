using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    private readonly ClubManagementDbContext _context;

    public EventRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<Event>> GetByClubIdAsync(int clubId)
    {
        return await _context.Events
            .Where(e => e.ClubId == clubId)
            .ToListAsync();
    }
}
