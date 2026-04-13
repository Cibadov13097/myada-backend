using MyAdaSupportService.Application.DTOs.Category;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<List<CategoryDto>> GetCategoriesByModuleAsync(string module);
    Task DeleteCategoryAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto dto);
}
