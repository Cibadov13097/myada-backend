using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IJobVacancyRepository : IGenericRepository<JobVacancy>
{
    Task<List<JobVacancy>> GetByClubIdAsync(int clubId);
}
