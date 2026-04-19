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
        return _mapper.Map<JobVacancyDto>(entity);
    }

    public async Task<JobVacancyDto> UpdateAsync(int id, UpdateJobVacancyDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobVacancy {id} not found.");
        _mapper.Map(dto, entity);
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
        return _mapper.Map<JobVacancyDto>(entity);
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
