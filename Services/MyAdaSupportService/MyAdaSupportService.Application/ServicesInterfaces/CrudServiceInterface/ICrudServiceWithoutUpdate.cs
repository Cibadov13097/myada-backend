using System.Linq.Expressions;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface ICrudServiceWithoutUpdate<TEntity, TListDto, TDetailDto, TCreateDto>
{
    Task<TDetailDto> GetByIdAsync(int id);

    Task<TDetailDto> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool asNoTracking = false,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null);

    Task<List<TListDto>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool asNoTracking = false,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);

    Task<TDetailDto> AddAsync(TCreateDto createDto);
    Task RemoveAsync(int id);
}
