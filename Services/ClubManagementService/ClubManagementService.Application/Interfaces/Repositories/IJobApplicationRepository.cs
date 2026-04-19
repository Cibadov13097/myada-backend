using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IJobApplicationRepository : IGenericRepository<JobApplication>
{
    Task<List<JobApplication>> GetByJobVacancyIdAsync(int jobVacancyId);
    Task<List<JobApplication>> GetByClubIdAsync(int clubId);
    Task<List<JobApplication>> GetByApplicantIdAsync(Guid applicantId);
}
