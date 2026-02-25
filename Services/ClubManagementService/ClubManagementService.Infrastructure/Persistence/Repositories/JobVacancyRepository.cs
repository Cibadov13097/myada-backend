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

    public async Task<List<JobVacancy>> GetByClubIdAsync(int clubId)
    {
        return await _context.JobVacancies
            .Where(v => v.ClubId == clubId)
            .ToListAsync();
    }
}
