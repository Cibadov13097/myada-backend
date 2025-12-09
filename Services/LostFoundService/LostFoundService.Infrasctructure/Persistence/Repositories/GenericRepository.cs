    using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Common;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T: AuditableEntity
    {
        protected readonly LostFoundDbContext _dbContext;

        protected readonly DbSet<T> _dbSet;
        public GenericRepository(LostFoundDbContext lostFoundDbContext)
        {
            _dbContext = lostFoundDbContext;
            _dbSet = _dbContext.Set<T>();

        }
        public async Task AddAsync(T entity)
        {
           await _dbSet.AddAsync(entity);
        }

        public  async Task DeleteAsync(T entity)
        {
             _dbSet.Remove(entity);
        }

        public async Task<List<T>> GetAllAsync()
        {
           return await _dbSet.Where(d=>!d.IsDeleted).ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(d=>d.Id==id && !d.IsDeleted);
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
