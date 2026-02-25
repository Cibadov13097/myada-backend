using AutoMapper;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class ClubService : IClubService
{
    private readonly IClubRepository _repository;
    private readonly IMapper _mapper;

    public ClubService(IClubRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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
        return _mapper.Map<ClubDto>(entity);
    }

    public async Task<ClubDto> UpdateAsync(int id, UpdateClubDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Club {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<ClubDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Club {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
