using AutoMapper;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Events.Notifications;
using System.Globalization;

namespace ClubManagementService.Infrastructure.Services;

public class EventRegistrationService : IEventRegistrationService
{
    private readonly IEventRegistrationRepository _repository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly IClubNotificationService _notificationService;
    private readonly ITicketJwtService _ticketJwt;
    private readonly ClubManagementDbContext _dbContext;

    public EventRegistrationService(
        IEventRegistrationRepository repository,
        IEventRepository eventRepository,
        IMapper mapper,
        IClubNotificationService notificationService,
        ITicketJwtService ticketJwt,
        ClubManagementDbContext dbContext)
    {
        _repository = repository;
        _eventRepository = eventRepository;
        _mapper = mapper;
        _notificationService = notificationService;
        _ticketJwt = ticketJwt;
        _dbContext = dbContext;
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
        // Legacy endpoint support: reuse the main registration flow so ticketing,
        // seat checks, and notification behavior stay consistent.
        return await RegisterAsync(dto.EventId, dto.UserId);
    }

    public async Task<EventRegistrationDto> UpdateAsync(int id, UpdateEventRegistrationDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"EventRegistration {id} not found.");

        _ = await _eventRepository.GetByIdAsync(dto.EventId)
            ?? throw new KeyNotFoundException($"Event {dto.EventId} was not found.");

        var duplicate = await _repository.GetByEventAndUserAsync(dto.EventId, entity.UserId);
        if (duplicate != null && duplicate.Id != entity.Id)
            throw new InvalidOperationException("Already registered for this event.");

        entity.EventId = dto.EventId;
        await _repository.UpdateAsync(entity);

        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            if (pg.SqlState == "23505")
                throw new InvalidOperationException("Already registered for this event.");
            if (pg.SqlState == "23503")
                throw new KeyNotFoundException($"Event {dto.EventId} was not found.");
            throw;
        }

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration Updated",
            Message = $"Registration '{entity.Id}' was updated to event '{entity.EventId}'.",
            Channel = "Push",
            Source = "club-management",
            Action = "event.registration.updated",
            EntityType = "event_registration",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.UserId,
            RecipientUserId = entity.UserId
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
            Channel = "Push",
            Source = "club-management",
            Action = "event.registration.deleted",
            EntityType = "event_registration",
            EntityId = id.ToString(),
            ActorUserId = entity.UserId,
            RecipientUserId = entity.UserId
        });
    }

    public async Task DeleteByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"No registration for event {eventId} and user.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration Cancelled",
            Message = $"A registration for event '{entity.EventId}' was cancelled.",
            Channel = "Push",
            Source = "club-management",
            Action = "event.registration.cancelled",
            EntityType = "event_registration",
            EntityId = entity.Id.ToString(),
            ActorUserId = userId,
            RecipientUserId = userId
        });
    }

    public async Task<EventRegistrationDto> RegisterAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId)
            ?? throw new KeyNotFoundException($"Event {eventId} was not found.");

        var existing = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return _mapper.Map<EventRegistrationDto>(existing);

        var current = (await _repository.GetByEventIdAsync(eventId)).Count;
        if (evt.SeatLimit > 0 && current >= evt.SeatLimit)
            throw new InvalidOperationException("This event has reached its seat limit.");

        var ticketId = $"TCK-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(180);
        var jwt = _ticketJwt.CreateTicketJwt(userId, eventId, ticketId, now, expiresAt);

        var entity = new EventRegistration
        {
            EventId = eventId,
            UserId = userId,
            TicketId = ticketId,
            RegistrationJwt = jwt,

            EventNameSnapshot = evt.Name,
            EventImageUrlSnapshot = evt.ImageUrl,
            EventStartTimeSnapshot = evt.StartTime,
            EventEndTimeSnapshot = evt.EndTime,
            EventLocationSnapshot = evt.Location,
            EventSeatLimitSnapshot = evt.SeatLimit,

            CreatedByUserId = userId
        };

        await _repository.AddAsync(entity);
        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            if (pg.SqlState == "23505")
            {
                // concurrent duplicate register (EventId,UserId unique) - return existing
                var dupe = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken).ConfigureAwait(false);
                if (dupe != null)
                    return _mapper.Map<EventRegistrationDto>(dupe);
                throw new InvalidOperationException("Already registered for this event.");
            }
            if (pg.SqlState == "23503")
                throw new KeyNotFoundException($"Event {eventId} was not found.");
            throw;
        }

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Event Registration",
            Message = $"You registered for event '{evt.Name}'.",
            Channel = "Push",
            Source = "club-management",
            Action = "event.registration.created",
            EntityType = "event_registration",
            EntityId = entity.Id.ToString(),
            ActorUserId = entity.UserId,
            RecipientUserId = entity.UserId
        });

        if (evt.SeatLimit > 0 && current + 1 >= evt.SeatLimit)
            await PublishEventCapacityFullNotificationAsync(evt, cancellationToken).ConfigureAwait(false);

        return _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task<List<EventRegistrationDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return _mapper.Map<List<EventRegistrationDto>>(list);
    }

    public async Task<EventRegistrationDto?> GetTicketAsync(int eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEventAndUserAsync(eventId, userId, cancellationToken).ConfigureAwait(false);
        return entity == null ? null : _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task<CheckInResult> CheckInAsync(int eventId, string jwt, Guid staffUserId, CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = _ticketJwt.ValidateTicketJwt(jwt, validateLifetime: true);
        }
        catch (SecurityTokenExpiredException)
        {
            return new CheckInResult(false, "expired_ticket", "Ticket token is expired.", "", Guid.Empty, eventId, null);
        }
        catch (Exception)
        {
            return new CheckInResult(false, "invalid_ticket", "Invalid ticket token.", "", Guid.Empty, eventId, null);
        }

        var claimEventId = principal.FindFirst("eventId")?.Value;
        var claimUserId = principal.FindFirst("userId")?.Value;
        var claimTicketId = principal.FindFirst("ticketId")?.Value;

        if (!int.TryParse(claimEventId, out var tokenEventId) || tokenEventId != eventId)
            return new CheckInResult(false, "event_mismatch", "Ticket does not belong to this event.", claimTicketId ?? "", Guid.Empty, eventId, null);
        if (!Guid.TryParse(claimUserId, out var attendeeId) || attendeeId == Guid.Empty || string.IsNullOrWhiteSpace(claimTicketId))
            return new CheckInResult(false, "invalid_ticket", "Ticket claims are missing.", claimTicketId ?? "", Guid.Empty, eventId, null);

        var reg = await _repository.GetByTicketIdAsync(claimTicketId, cancellationToken).ConfigureAwait(false);
        if (reg == null)
            return new CheckInResult(false, "ticket_not_found", "Ticket not found.", claimTicketId, attendeeId, eventId, null);
        if (reg.EventId != eventId || reg.UserId != attendeeId)
            return new CheckInResult(false, "invalid_ticket", "Ticket does not match registration.", claimTicketId, attendeeId, eventId, null);

        var now = DateTime.UtcNow;

        reg.LastScannedAt = now;
        reg.ScanCount = reg.ScanCount + 1;

        if (reg.IsCheckedIn)
        {
            await _repository.SaveChangesAsync();
            return new CheckInResult(false, "already_checked_in", "This ticket was already used.", reg.TicketId, attendeeId, eventId, reg.CheckedInAt);
        }

        reg.IsCheckedIn = true;
        reg.CheckedInAt = now;
        reg.CheckedInBy = staffUserId;

        await _repository.SaveChangesAsync();
        return new CheckInResult(true, "checked_in", "Entry allowed", reg.TicketId, attendeeId, eventId, reg.CheckedInAt);
    }

    private async Task PublishEventCapacityFullNotificationAsync(Event evt, CancellationToken cancellationToken)
    {
        var clubAdminUserIds = await _dbContext.ClubEmployees
            .AsNoTracking()
            .Where(e => e.ClubId == evt.ClubId)
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var recipientUserId in clubAdminUserIds.Where(id => id != Guid.Empty))
        {
            await _notificationService.PublishAsync(new NotificationQueueMessage
            {
                Type = "All seats are taken for the event",
                Message = evt.Name,
                Channel = "Push",
                Source = "club-management",
                Action = "event.capacity_full",
                EntityType = "event",
                EntityId = evt.Id.ToString(CultureInfo.InvariantCulture),
                RecipientUserId = recipientUserId
            }).ConfigureAwait(false);
        }
    }
}
