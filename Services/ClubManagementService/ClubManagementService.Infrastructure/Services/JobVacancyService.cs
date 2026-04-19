using AutoMapper;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Infrastructure.Services;

public class JobVacancyService : IJobVacancyService
{
    private readonly IJobVacancyRepository _repository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;

    public JobVacancyService(IJobVacancyRepository repository, IMapper mapper, IClubNotificationService notificationService)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
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
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Created",
            Message = $"Vacancy '{entity.Title}' was created.",
            Source = "club-management",
            Action = "vacancy.created",
            EntityType = "vacancy",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.PostedBy
        });
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
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Updated",
            Message = $"Vacancy '{entity.Title}' was updated.",
            Source = "club-management",
            Action = "vacancy.updated",
            EntityType = "vacancy",
            EntityId = entity.Id.ToString()
        });
        var withClub = await _repository.GetByIdWithClubAsync(id) ?? entity;
        return _mapper.Map<JobVacancyDto>(withClub);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        var vacancyTitle = entity.Title;
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Deleted",
            Message = $"Vacancy '{vacancyTitle}' was deleted.",
            Source = "club-management",
            Action = "vacancy.deleted",
            EntityType = "vacancy",
            EntityId = id.ToString()
        });
    }
}
