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

    public async Task<List<EventRegistrationDto>> GetByEventIdAsync(Guid eventId)
    {
        var list = await _repository.GetByEventIdAsync(eventId);
        return _mapper.Map<List<EventRegistrationDto>>(list);
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
}
