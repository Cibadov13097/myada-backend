using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface ILostItemService
    {
        Task<LostItem> GetLostItemByIdAsync(int id);

        Task<LostItem> GetMyLostItemAsync(string id);
        Task<List<LostItem>> GetAllLostItemsAsync();

        Task AddLostItemAsync(LostItem entity);

        Task UpdateLostItemAsync(LostItem entity);
    }
}
