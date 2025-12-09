using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface IFoundItemService
    {
        // Public endpoints
        Task<FoundItem> GetFoundItemByIdAsync(int id);
        Task<List<FoundItem>> GetAllFoundItemsAsync();

        // User-specific endpoints
        Task<List<FoundItem>> GetMyFoundItemsAsync(string userId);
        Task<FoundItem> GetMyFoundItemByIdAsync(int id, string userId);


        // Mutations
        Task AddFoundItemAsync(FoundItem entity, string userId);
        Task UpdateFoundItemAsync(FoundItem entity, string userId);
        Task RemoveFoundItemAsync(int id, string userId);
    }

}
