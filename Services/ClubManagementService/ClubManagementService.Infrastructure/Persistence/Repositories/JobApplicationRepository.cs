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

    public async Task<List<JobApplication>> GetByClubIdAsync(int clubId)
    {
        return await _context.JobApplications
            .Where(a => _context.JobVacancies.Any(v => v.Id == a.JobVacancyId && v.ClubId == clubId))
            .ToListAsync();
    }

    public async Task<List<JobApplication>> GetByApplicantIdAsync(Guid applicantId)
    {
        return await _context.JobApplications
            .Include(a => a.JobVacancy!)
            .ThenInclude(v => v.Club)
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
