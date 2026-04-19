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
        var entity = await _repository.GetByIdWithClubAsync(id);
        return entity == null ? null : _mapper.Map<JobVacancyDto>(entity);
    }

    public async Task<List<JobVacancyDto>> GetAllAsync()
    {
        var list = await _repository.GetAllWithClubAsync();
        var active = list.Where(v => !v.IsDraft && v.IsActive).ToList();
        return _mapper.Map<List<JobVacancyDto>>(active);
    }

    public async Task<List<JobVacancyDto>> GetByClubIdAsync(int clubId, bool activeOnly = false)
    {
        var list = await _repository.GetByClubIdAsync(clubId, activeOnly: false);
        var filtered = list.AsEnumerable();
        if (activeOnly)
            filtered = filtered.Where(v => !v.IsDraft && v.IsActive);
        return _mapper.Map<List<JobVacancyDto>>(filtered.ToList());
    }

    public async Task<JobVacancyDto> CreateAsync(CreateJobVacancyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) && (dto.ClubPositionId is null || dto.ClubPositionId <= 0))
            throw new ArgumentException("Either a vacancy title or a linked position is required.");

        var entity = _mapper.Map<JobVacancy>(dto);
        entity.IsDraft = !dto.Publish;
        entity.IsActive = dto.Publish;
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        var withClub = await _repository.GetByIdWithClubAsync(entity.Id) ?? entity;
        return _mapper.Map<JobVacancyDto>(withClub);
    }

    public async Task<JobVacancyDto> UpdateAsync(int id, UpdateJobVacancyDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        if (dto.Title != null)
            entity.Title = dto.Title;
        if (dto.Description != null)
            entity.Description = dto.Description;
        if (dto.IsActive.HasValue)
            entity.IsActive = dto.IsActive.Value;
        if (dto.ClubPositionId.HasValue)
            entity.ClubPositionId = dto.ClubPositionId;
        if (dto.ApplicationDeadline.HasValue)
            entity.ApplicationDeadline = dto.ApplicationDeadline;
        if (dto.IsDraft.HasValue)
            entity.IsDraft = dto.IsDraft.Value;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        var withClub = await _repository.GetByIdWithClubAsync(id) ?? entity;
        return _mapper.Map<JobVacancyDto>(withClub);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
