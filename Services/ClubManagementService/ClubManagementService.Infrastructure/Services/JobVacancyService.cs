using AutoMapper;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class JobVacancyService : IJobVacancyService
{
    private readonly IJobVacancyRepository _repository;
    private readonly IMapper _mapper;

    public JobVacancyService(IJobVacancyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<JobVacancyDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<JobVacancyDto>(entity);
    }

    public async Task<List<JobVacancyDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<JobVacancyDto>>(list);
    }

    public async Task<List<JobVacancyDto>> GetByClubIdAsync(int clubId)
    {
        var list = await _repository.GetByClubIdAsync(clubId);
        return _mapper.Map<List<JobVacancyDto>>(list);
    }

    public async Task<JobVacancyDto> CreateAsync(CreateJobVacancyDto dto)
    {
        var entity = _mapper.Map<JobVacancy>(dto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<JobVacancyDto>(entity);
    }

    public async Task<JobVacancyDto> UpdateAsync(int id, UpdateJobVacancyDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<JobVacancyDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
