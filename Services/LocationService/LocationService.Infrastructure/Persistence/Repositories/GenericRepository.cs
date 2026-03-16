using LocationService.Application.Interfaces.Repositories;
using LocationService.Domain.Common;
using LocationService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationService.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T>
        where T : AuditableEntity
    {
        protected readonly LocationDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(LocationDbContext locationDbContext)
        {
            _dbContext = locationDbContext;
            _dbSet= _dbContext.Set<T>();
        }
        public async Task AddAsync(T entity)
        {
            _dbSet.Add(entity);
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);  
        }

        public async Task<List<T>> GetAllAsync()
        {
           return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(d=>d.Id==id);
        }

        public async Task SaveChangesAsync()
        {
           await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
             _dbSet.Update(entity);
        }
    }
}
