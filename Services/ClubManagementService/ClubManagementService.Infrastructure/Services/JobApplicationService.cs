using System.Globalization;
using AutoMapper;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Infrastructure.Services;

public class JobApplicationService : IJobApplicationService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IMapper _mapper;

    public JobApplicationService(IJobApplicationRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<JobApplicationDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<JobApplicationDto>(entity);
    }

    public async Task<List<JobApplicationDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<JobApplicationDto>>(list);
    }

    public async Task<List<JobApplicationDto>> GetByJobVacancyIdAsync(int jobVacancyId)
    {
        var list = await _repository.GetByJobVacancyIdAsync(jobVacancyId);
        return _mapper.Map<List<JobApplicationDto>>(list);
    }

    public async Task<List<JobApplicationDto>> GetByClubIdAsync(int clubId)
    {
        var list = await _repository.GetByClubIdAsync(clubId);
        return _mapper.Map<List<JobApplicationDto>>(list);
    }

    public async Task<List<MyVacancyApplicationDto>> GetMyVacancyApplicationsAsync(Guid applicantId)
    {
        var list = await _repository.GetByApplicantIdAsync(applicantId);
        return list.Select(a => new MyVacancyApplicationDto
        {
            ApplicationId = $"job:{a.Id.ToString(CultureInfo.InvariantCulture)}",
            VacancyId = a.JobVacancyId.ToString(CultureInfo.InvariantCulture),
            VacancyTitle = a.JobVacancy?.Title,
            ClubName = a.JobVacancy?.Club?.Name,
            Status = FormatApplicationStatus(a.Status),
            SubmittedAt = a.CreatedAt
        }).ToList();
    }

    private static string FormatApplicationStatus(JobApplicationStatus status) => status switch
    {
        JobApplicationStatus.Pending => "Pending",
        JobApplicationStatus.Reviewing => "Reviewing",
        JobApplicationStatus.Approved => "Approved",
        JobApplicationStatus.Rejected => "Rejected",
        JobApplicationStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public async Task<JobApplicationDto> CreateAsync(CreateJobApplicationDto dto)
    {
        var entity = _mapper.Map<JobApplication>(dto);
        entity.Status = Domain.Enums.JobApplicationStatus.Pending;
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<JobApplicationDto>(entity);
    }

    public async Task<JobApplicationDto> UpdateStatusAsync(int id, UpdateJobApplicationStatusDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobApplication {id} not found.");
        entity.Status = dto.Status;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<JobApplicationDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobApplication {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
