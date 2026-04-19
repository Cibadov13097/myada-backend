using AutoMapper;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class EventRegistrationService : IEventRegistrationService
{
    private readonly IEventRegistrationRepository _repository;
    private readonly IMapper _mapper;

    public EventRegistrationService(IEventRegistrationRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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
        var entity = _mapper.Map<EventRegistration>(dto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<EventRegistrationDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"EventRegistration {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
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
