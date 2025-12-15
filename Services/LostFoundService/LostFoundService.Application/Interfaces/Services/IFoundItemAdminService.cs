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
    public interface IFoundItemAdminService
    {
        //Admin
        Task<List<AdminFoundItemDto>> AdminGetAllAsync(bool includeDeleted);

        Task<AdminFoundItemDto> AdminGetByIdAsync(int id);
        Task<GetFoundItemDto> AdminUpdateStatusAsync(int itemId, Status status, string adminId);
        Task<GetFoundItemDto> AdminAddFoundItemAsync(AddFoundItemDto dto, string adminId);
        Task AdminDeleteAsync(int itemId, string adminId);
        Task AdminForceDeleteAsync(int itemId);
        Task<GetFoundItemDto> AdminUpdateAsync(int itemId, UpdateFoundItemDto dto, string adminId);
    }
}
