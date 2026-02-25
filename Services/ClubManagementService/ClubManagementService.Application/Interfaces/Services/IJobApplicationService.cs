using ClubManagementService.Application.DTOs.JobApplication;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IJobApplicationService
{
    Task<JobApplicationDto?> GetByIdAsync(int id);
    Task<List<JobApplicationDto>> GetAllAsync();
    Task<List<JobApplicationDto>> GetByJobVacancyIdAsync(int jobVacancyId);
    Task<JobApplicationDto> CreateAsync(CreateJobApplicationDto dto);
    Task<JobApplicationDto> UpdateStatusAsync(int id, UpdateJobApplicationStatusDto dto);
    Task DeleteAsync(int id);
}
