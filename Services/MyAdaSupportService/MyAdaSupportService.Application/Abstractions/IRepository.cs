using MyAdaSupportService.Domain.Entities.BaseAggregate;
using System.Linq.Expressions;

namespace MyAdaSupportService.Application.Abstractions;

public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(int id);

    Task<T> GetAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = false);

    Task<List<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false);

    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task RemoveAsync(T entity);
}
