using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class ClubMembershipRepository : GenericRepository<ClubMembership>, IClubMembershipRepository
{
    private readonly ClubManagementDbContext _context;

    public ClubMembershipRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<int>> GetClubIdsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ClubMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.ClubId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetUserIdsByClubAsync(int clubId, CancellationToken cancellationToken = default)
    {
        return await _context.ClubMemberships
            .AsNoTracking()
            .Where(m => m.ClubId == clubId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
