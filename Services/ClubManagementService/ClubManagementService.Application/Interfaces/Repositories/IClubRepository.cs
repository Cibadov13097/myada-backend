using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IClubRepository : IGenericRepository<Club>
{
    Task<Club?> GetByIdWithCategoryAsync(int id);
    Task<List<Club>> GetByCategoryIdAsync(int categoryId);
}
