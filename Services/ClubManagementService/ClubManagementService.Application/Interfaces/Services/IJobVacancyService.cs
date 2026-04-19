using ClubManagementService.Application.DTOs.JobVacancy;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IJobVacancyService
{
    Task<JobVacancyDto?> GetByIdAsync(int id);
    Task<List<JobVacancyDto>> GetAllAsync();
    Task<List<JobVacancyDto>> GetByClubIdAsync(int clubId, bool activeOnly = false);
    Task<JobVacancyDto> CreateAsync(CreateJobVacancyDto dto);
    Task<JobVacancyDto> UpdateAsync(int id, UpdateJobVacancyDto dto);
    Task DeleteAsync(int id);
}
