using AutoMapper;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Infrastructure.Services;

public class EventRegistrationService : IEventRegistrationService
{
    private readonly IEventRegistrationRepository _repository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;

    public EventRegistrationService(
        IEventRegistrationRepository repository,
        IEventRepository eventRepository,
        IMapper mapper,
        IClubNotificationService notificationService)
    {
        _repository = repository;
        _eventRepository = eventRepository;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<EventRegistrationDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task<List<EventRegistrationDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<EventRegistrationDto>>(list);
    }

    public async Task<List<EventRegistrationDto>> GetByEventIdAsync(int eventId)
    {
        var list = await _repository.GetByEventIdAsync(eventId);
        return _mapper.Map<List<EventRegistrationDto>>(list);
    }

    public async Task<EventRegistrationDto?> GetByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken);
        return entity == null ? null : _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task<EventRegistrationDto> CreateAsync(CreateEventRegistrationDto dto)
    {
        _ = await _eventRepository.GetByIdAsync(dto.EventId)
            ?? throw new KeyNotFoundException($"Event {dto.EventId} was not found.");

        var duplicate = await _repository.GetByEventAndUserAsync(dto.EventId, dto.UserId);
        if (duplicate != null)
            throw new InvalidOperationException("Already registered for this event.");

        var entity = _mapper.Map<EventRegistration>(dto);
        await _repository.AddAsync(entity);
        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            // 23505 = unique_violation (e.g. concurrent duplicate), 23503 = foreign_key_violation
            if (pg.SqlState == "23505")
                throw new InvalidOperationException("Already registered for this event.");
            if (pg.SqlState == "23503")
                throw new KeyNotFoundException($"Event {dto.EventId} was not found.");
            throw;
        }

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration",
            Message = $"A user registered for event '{entity.EventId}'.",
            Source = "club-management",
            Action = "event.registration.created",
            EntityType = "event_registration",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.UserId
        });
        return _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"EventRegistration {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration Cancelled",
            Message = $"A registration for event '{entity.EventId}' was cancelled.",
            Source = "club-management",
            Action = "event.registration.deleted",
            EntityType = "event_registration",
            EntityId = id.ToString(),
            ActorUserId = entity.UserId
        });
    }

    public async Task DeleteByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"No registration for event {eventId} and user.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
