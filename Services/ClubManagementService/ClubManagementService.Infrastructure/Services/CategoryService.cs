using AutoMapper;
using ClubManagementService.Application.DTOs.Category;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<CategoryDto>(entity);
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return _mapper.Map<List<CategoryDto>>(list);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var entity = _mapper.Map<Category>(dto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(entity);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Category {id} not found.");
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Category {id} not found.");
        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
    }
}
