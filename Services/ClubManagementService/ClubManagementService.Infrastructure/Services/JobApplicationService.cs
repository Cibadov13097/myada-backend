using AutoMapper;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

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
