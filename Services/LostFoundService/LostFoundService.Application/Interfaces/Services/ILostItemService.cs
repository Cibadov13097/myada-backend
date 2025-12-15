using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
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
        // Public
        Task<List<AdminLostItemDto>> GetAllLostItemsAsync();
        Task<AdminLostItemDto> GetLostItemByIdAsync(int id);


        // User scoped
        Task<List<AdminLostItemDto>> GetMyLostItemsAsync(string userId);
        Task<AdminLostItemDto> GetMyLostItemByIdAsync(int id, string userId);


        // Mutations
        Task<AdminLostItemDto> AddLostItemAsync(AddLostItemDto dto, string userId);
        Task<AdminLostItemDto> UpdateLostItemAsync(int id, UpdateLostItemDto dto, string userId);
        Task DeleteLostItemAsync(int id, string userId);
    }
}