using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Domain.Entities.BaseAggregate;
using MyAdaSupportService.Application.Abstractions;
using System.Linq;
using System.Linq.Expressions;

namespace MyAdaSupportService.Infrastructure.Repositories;

public class EfCoreRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EfCoreRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<T> GetByIdAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
            throw new KeyNotFoundException($"{typeof(T).Name} with id {id} was not found.");

        return entity;
    }

    public virtual async Task<T> GetAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _dbSet;

        if (asNoTracking)
            query = query.AsNoTracking();

        if (include != null)
            query = include(query);

        var entity = await query.FirstOrDefaultAsync(predicate);

        if (entity == null)
            throw new KeyNotFoundException($"{typeof(T).Name} was not found.");

        return entity;
    }

    public virtual async Task<List<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _dbSet;

        if (asNoTracking)
            query = query.AsNoTracking();

        if (include != null)
            query = include(query);

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        return await query.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity is IAuditedEntity auditedEntity)
        {
            if (auditedEntity.CreatedAt == default)
                auditedEntity.CreatedAt = DateTime.UtcNow;

            auditedEntity.ModifiedAt = null;
            auditedEntity.ModifiedUserId = null;
        }

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task RemoveAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
