using AutoMapper;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IMapper _mapper;

    public EventService(IEventRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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
        var entity = _mapper.Map<Event>(dto);
        entity.Status = Domain.Enums.ClubStatus.Pending;
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<EventDto>(entity);
    }

    public async Task<EventDto> UpdateAsync(int id, UpdateEventDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Event {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<EventDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Event {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
