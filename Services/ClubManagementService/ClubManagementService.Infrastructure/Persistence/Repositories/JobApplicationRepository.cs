using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class JobApplicationRepository : GenericRepository<JobApplication>, IJobApplicationRepository
{
    private readonly ClubManagementDbContext _context;

    public JobApplicationRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
        _context = dbContext;
    }

    public async Task<List<JobApplication>> GetByJobVacancyIdAsync(int jobVacancyId)
    {
        return await _context.JobApplications
            .Where(a => a.JobVacancyId == jobVacancyId)
            .ToListAsync();
    }
}
