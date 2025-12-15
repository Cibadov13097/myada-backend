using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface ILostItemAdminService
    {
        Task<List<AdminLostItemDto>> AdminGetAllAsync(bool includeDeleted);
        Task<AdminLostItemDto> AdminGetByIdAsync(int id);

        Task<AdminLostItemDto> AdminAddLostItemAsync(AddLostItemDto dto, string adminId);
        Task<AdminLostItemDto> AdminUpdateAsync(int itemId, UpdateLostItemDto dto, string adminId);

        Task AdminDeleteAsync(int itemId, string adminId);
        Task AdminForceDeleteAsync(int itemId);
    }
}
