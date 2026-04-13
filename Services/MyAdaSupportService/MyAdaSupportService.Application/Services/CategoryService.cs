using AutoMapper;
using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.Services;

public class CategoryService
    : CrudManager<Category, CategoryDto, CategoryDto, CategoryCreateDto, CategoryUpdateDto>,
      ICategoryCrudService,
      ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        : base(categoryRepository, mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        return await GetAllAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesByModuleAsync(string module)
    {
        var categories = await _categoryRepository.GetByModuleAsync(module);
        return _mapper.Map<List<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto dto)
    {
        return await AddAsync(dto);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await RemoveAsync(id);
    }
}