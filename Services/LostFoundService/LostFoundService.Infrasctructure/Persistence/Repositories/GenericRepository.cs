using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Common;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T>
        where T : AuditableEntity
    {
        protected readonly LostFoundDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(LostFoundDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

      
        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
        }

      
        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
