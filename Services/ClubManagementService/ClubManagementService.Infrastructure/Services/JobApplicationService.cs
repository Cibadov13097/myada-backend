using AutoMapper;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Infrastructure.Services;

public class JobApplicationService : IJobApplicationService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;

    public JobApplicationService(IJobApplicationRepository repository, IMapper mapper, IClubNotificationService notificationService)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
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
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Application",
            Message = $"A new application was submitted for vacancy '{entity.JobVacancyId}'.",
            Source = "club-management",
            Action = "job_application.created",
            EntityType = "job_application",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.ApplicantId
        });
        return _mapper.Map<JobApplicationDto>(entity);
    }

    public async Task<JobApplicationDto> UpdateStatusAsync(int id, UpdateJobApplicationStatusDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobApplication {id} not found.");
        entity.Status = dto.Status;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Application Status",
            Message = $"Application '{entity.Id}' status changed to '{entity.Status}'.",
            Source = "club-management",
            Action = "job_application.status_updated",
            EntityType = "job_application",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.ApplicantId
        });
        return _mapper.Map<JobApplicationDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"JobApplication {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Vacancy Application Deleted",
            Message = $"Application '{id}' was deleted.",
            Source = "club-management",
            Action = "job_application.deleted",
            EntityType = "job_application",
            EntityId = id.ToString(),
            ActorUserId = entity.ApplicantId
        });
    }
}
