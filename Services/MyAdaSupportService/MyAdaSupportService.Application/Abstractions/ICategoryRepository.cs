using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.Abstractions;

public interface ICategoryRepository : IRepository<Category>
{
    Task<List<Category>> GetByModuleAsync(string module);
    Task<Category?> GetByNameAsync(string name);
}
