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
    }
}
