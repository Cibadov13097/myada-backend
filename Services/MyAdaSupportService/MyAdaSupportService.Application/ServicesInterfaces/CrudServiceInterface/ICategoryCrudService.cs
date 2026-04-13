using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface ICategoryCrudService
    : ICrudService<Category, CategoryDto, CategoryDto, CategoryCreateDto, CategoryUpdateDto>
{
}
