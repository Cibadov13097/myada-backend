using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
    }
}
