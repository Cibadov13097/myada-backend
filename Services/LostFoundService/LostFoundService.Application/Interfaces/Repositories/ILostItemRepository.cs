using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Repositories
{
    public interface ILostItemRepository : IGenericRepository<LostItem>
    {
        Task<List<LostItem>> GetAllByUserIdAsync(string userId);
        Task<LostItem?> GetByIdForUserAsync(int id, string userId);
        Task<LostItem?> GetByIdForAdminAsync(int id);
        Task<List<LostItem>> GetAllForAdminAsync(bool includeDeleted = true);
    }
}
