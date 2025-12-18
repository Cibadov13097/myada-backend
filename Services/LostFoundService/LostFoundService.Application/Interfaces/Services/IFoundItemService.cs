    using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Domain.Entities;
    using LostFoundService.Domain.Enums;
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
            Task<AdminGetFoundItemDto> GetFoundItemByIdAsync(int id);
            Task<List<GetFoundItemDto>> GetAllFoundItemsAsync();

            // User-specific endpoints
            Task<List<GetFoundItemDto>> GetMyFoundItemsAsync(string userId);
            Task<GetFoundItemDto> GetMyFoundItemByIdAsync(int id, string userId);


            // Mutations
            Task<GetFoundItemDto> AddFoundItemAsync(AddFoundItemDto dto, string userId);
            Task<GetFoundItemDto> UpdateFoundItemAsync(int itemId, UpdateFoundItemDto entity, string userId);
            Task DeleteFoundItemAsync(int itemId, string userId);

       

        }

    }
