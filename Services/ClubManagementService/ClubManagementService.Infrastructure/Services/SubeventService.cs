using AutoMapper;
using ClubManagementService.Application.DTOs.Subevent;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class SubeventService : ISubeventService
{
    private readonly ISubeventRepository _repository;
    private readonly IMapper _mapper;

    public SubeventService(ISubeventRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<SubeventDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<SubeventDto>(entity);
    }

    public async Task<List<SubeventDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<SubeventDto>>(list);
    }

    public async Task<List<SubeventDto>> GetByMainEventIdAsync(int mainEventId)
    {
        var list = await _repository.GetByMainEventIdAsync(mainEventId);
        return _mapper.Map<List<SubeventDto>>(list);
    }

    public async Task<SubeventDto> CreateAsync(CreateSubeventDto dto)
    {
        var entity = _mapper.Map<Subevent>(dto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<SubeventDto>(entity);
    }

    public async Task<SubeventDto> UpdateAsync(int id, UpdateSubeventDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Subevent {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<SubeventDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Subevent {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
