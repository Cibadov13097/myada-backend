using AutoMapper;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Infrastructure.Services;

public class ClubService : IClubService
{
    private readonly IClubRepository _repository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;

    public ClubService(IClubRepository repository, IMapper mapper, IClubNotificationService notificationService)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<ClubDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdWithCategoryAsync(id);
        return entity == null ? null : _mapper.Map<ClubDto>(entity);
    }

    public async Task<List<ClubDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<ClubDto>>(list);
    }

    public async Task<List<ClubDto>> GetByCategoryIdAsync(int categoryId)
    {
        var list = await _repository.GetByCategoryIdAsync(categoryId);
        return _mapper.Map<List<ClubDto>>(list);
    }

    public async Task<ClubDto> CreateAsync(CreateClubDto dto)
    {
        var entity = _mapper.Map<Club>(dto);
        entity.Status = Domain.Enums.ClubStatus.Pending;
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal",
            Message = $"Club '{entity.Name}' was submitted for approval.",
            Source = "club-management",
            Action = "club.created",
            EntityType = "club",
            EntityId = entity.Id.ToString(),
            ActorUserId = dto.ApprovedBy
        });
        return _mapper.Map<ClubDto>(entity);
    }

    public async Task<ClubDto> UpdateAsync(int id, UpdateClubDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Club {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Updated",
            Message = $"Club '{entity.Name}' was updated.",
            Source = "club-management",
            Action = "club.updated",
            EntityType = "club",
            EntityId = entity.Id.ToString()
        });
        return _mapper.Map<ClubDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Club {id} not found.");
        var clubName = entity.Name;
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Deleted",
            Message = $"Club '{clubName}' was deleted.",
            Source = "club-management",
            Action = "club.deleted",
            EntityType = "club",
            EntityId = id.ToString()
        });
    }
}
