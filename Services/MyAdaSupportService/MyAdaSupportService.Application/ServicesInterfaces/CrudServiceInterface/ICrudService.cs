using System.Linq.Expressions;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface ICrudService<TEntity, TListDto, TDetailDto, TCreateDto, TUpdateDto> : ICrudServiceWithoutUpdate<TEntity, TListDto, TDetailDto, TCreateDto>
{
    Task<TDetailDto?> UpdateAsync(int id, TUpdateDto updateDto);
}