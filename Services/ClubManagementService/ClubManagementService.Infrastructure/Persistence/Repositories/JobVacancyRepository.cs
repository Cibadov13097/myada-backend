using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class JobVacancyRepository : GenericRepository<JobVacancy>, IJobVacancyRepository
{
    private readonly ClubManagementDbContext _context;

    public JobVacancyRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<JobVacancy>> GetAllWithClubAsync()
    {
        return await _context.JobVacancies
            .Include(v => v.Club)
            .Include(v => v.ClubPosition)
            .ToListAsync();
    }

    public async Task<JobVacancy?> GetByIdWithClubAsync(int id)
    {
        return await _context.JobVacancies
            .Include(v => v.Club)
            .Include(v => v.ClubPosition)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<JobVacancy>> GetByClubIdAsync(int clubId, bool activeOnly = false)
    {
        var q = _context.JobVacancies
            .Include(v => v.Club)
            .Include(v => v.ClubPosition)
            .Where(v => v.ClubId == clubId);
        if (activeOnly)
            q = q.Where(v => v.IsActive);
        return await q.ToListAsync();
    }
}
