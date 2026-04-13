using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Infrastructure.Repositories;

public class CategoryRepository
    : EfCoreRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Category>> GetByModuleAsync(string module)
    {
        if (!Enum.TryParse<SupportArea>(module, true, out var parsedModule))
            return new List<Category>();

        return await _dbSet
            .Where(c => c.Module == parsedModule)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }
}