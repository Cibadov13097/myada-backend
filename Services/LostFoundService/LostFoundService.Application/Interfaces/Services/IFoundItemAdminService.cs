using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Domain.Enums;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface IFoundItemAdminService
    {
        Task<List<AdminGetFoundItemDto>> GetAllAsync(bool includeDeleted);
        Task<AdminGetFoundItemDto> GetByIdAsync(int id);

        Task<List<GetFoundItemDto>> GetByCategoryIdAsync(int categoryId);
        Task<GetFoundItemDto> AddAsync(AddFoundItemDto dto, string adminId);
        Task<GetFoundItemDto> UpdateAsync(int itemId, UpdateFoundItemDto dto, string adminId);
        Task<GetFoundItemDto> UpdateStatusAsync(int itemId, Status status, string adminId);
        Task DeleteAsync(int itemId, string adminId);
        Task ForceDeleteAsync(int itemId);
    }
}
