using AutoMapper;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;

    public EventService(IEventRepository repository, IMapper mapper, IClubNotificationService notificationService)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<EventDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<EventDto>(entity);
    }

    public async Task<List<EventDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<EventDto>>(list);
    }

    public async Task<List<EventDto>> GetByClubIdAsync(int clubId)
    {
        var list = await _repository.GetByClubIdAsync(clubId);
        return _mapper.Map<List<EventDto>>(list);
    }

    public async Task<EventDto> CreateAsync(CreateEventDto dto)
    {
        NormalizeAndValidate(dto);
        ValidateLocationIds(dto.BuildingId, dto.RoomId);
        var entity = _mapper.Map<Event>(dto);
        entity.Status = ClubStatus.Pending;
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Created",
            Message = $"Event '{entity.Name}' was created.",
            Source = "club-management",
            Action = "event.created",
            EntityType = "event",
            EntityId = entity.Id.ToString(),
            ActorUserId = dto.ApprovedBy
        });
        return _mapper.Map<EventDto>(entity);
    }

    public async Task<EventDto> UpdateAsync(int id, UpdateEventDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Event {id} not found.");
        NormalizeAndValidate(dto);
        ValidateLocationIds(dto.BuildingId, dto.RoomId);
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Updated",
            Message = $"Event '{entity.Name}' was updated.",
            Source = "club-management",
            Action = "event.updated",
            EntityType = "event",
            EntityId = entity.Id.ToString()
        });
        return _mapper.Map<EventDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Event {id} not found.");
        var eventName = entity.Name;
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Deleted",
            Message = $"Event '{eventName}' was deleted.",
            Source = "club-management",
            Action = "event.deleted",
            EntityType = "event",
            EntityId = id.ToString()
        });
    }

    private static void ValidateLocationIds(int? buildingId, int? roomId)
    {
        if (buildingId.HasValue != roomId.HasValue)
            throw new ArgumentException("buildingId and roomId must be provided together.");
        if ((buildingId.HasValue && buildingId <= 0) || (roomId.HasValue && roomId <= 0))
            throw new ArgumentException("buildingId and roomId must be positive integers.");
    }

    private static void NormalizeAndValidate(CreateEventDto dto)
    {
        dto.Name = NormalizeRequiredText(dto.Name, "name");
        dto.Description = NormalizeRequiredText(dto.Description, "description");
        dto.StartTime = NormalizeUtc(dto.StartTime);
        dto.EndTime = NormalizeUtc(dto.EndTime);
        dto.Location = NormalizeRequiredText(dto.Location, "location");
        dto.Requirements = NormalizeOptionalText(dto.Requirements);
        dto.Prerequisites = NormalizeOptionalText(dto.Prerequisites);
        dto.Notes = NormalizeOptionalText(dto.Notes);
        dto.ImageUrl = NormalizeOptionalUrl(dto.ImageUrl);
        ValidateSharedRules(dto.StartTime, dto.EndTime, dto.SeatLimit);
    }

    private static void NormalizeAndValidate(UpdateEventDto dto)
    {
        dto.Name = NormalizeRequiredText(dto.Name, "name");
        dto.Description = NormalizeRequiredText(dto.Description, "description");
        dto.StartTime = NormalizeUtc(dto.StartTime);
        dto.EndTime = NormalizeUtc(dto.EndTime);
        dto.Location = NormalizeRequiredText(dto.Location, "location");
        dto.Requirements = NormalizeOptionalText(dto.Requirements);
        dto.Prerequisites = NormalizeOptionalText(dto.Prerequisites);
        dto.Notes = NormalizeOptionalText(dto.Notes);
        dto.ImageUrl = NormalizeOptionalUrl(dto.ImageUrl);
        if (!Enum.IsDefined(dto.Status))
            throw new ArgumentException("status is invalid.");

        ValidateSharedRules(dto.StartTime, dto.EndTime, dto.SeatLimit);
    }

    private static void ValidateSharedRules(DateTime startTime, DateTime endTime, int seatLimit)
    {
        if (startTime == default)
            throw new ArgumentException("startTime is required.");
        if (endTime == default)
            throw new ArgumentException("endTime is required.");
        if (endTime <= startTime)
            throw new ArgumentException("endTime must be after startTime.");
        if (seatLimit <= 0)
            throw new ArgumentException("seatLimit must be greater than 0.");
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException($"{fieldName} is required.");

        return normalized;
    }

    private static string NormalizeOptionalText(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptionalUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
