using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IJobVacancyRepository : IGenericRepository<JobVacancy>
{
    Task<List<JobVacancy>> GetAllWithClubAsync();
    Task<JobVacancy?> GetByIdWithClubAsync(int id);
    Task<List<JobVacancy>> GetByClubIdAsync(int clubId, bool activeOnly = false);
}
