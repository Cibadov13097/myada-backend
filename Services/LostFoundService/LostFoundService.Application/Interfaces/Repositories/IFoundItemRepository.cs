using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Repositories
{
    public interface IFoundItemRepository: IGenericRepository<FoundItem>
    {
        Task<List<FoundItem>> GetAllByUserIdAsync(string userId);
        Task<FoundItem> GetByIdForUserAsync(int id, string userId);

        Task<FoundItem> GetByIdForAdminAsync(int id);

        Task<List<FoundItem>> GetAllForAdminAsync(bool includeDeleted = true);

        Task<List<FoundItem>> GetAllAsync(bool includeDeleted);
        Task<FoundItem?> GetByIdAsync(int id);

        Task<List<FoundItem>> GetByCategoryIdAsync(
        int categoryId,
        bool includeDeleted = false
        );
    }
}
