using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class EventRegistrationRepository : GenericRepository<EventRegistration>, IEventRegistrationRepository
{
    private readonly ClubManagementDbContext _context;

    public EventRegistrationRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<EventRegistration>> GetByEventIdAsync(int eventId)
    {
        return await _context.EventRegistrations
            .Where(r => r.EventId == eventId)
            .ToListAsync();
    }

    public async Task<EventRegistration?> GetByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);
    }
}
