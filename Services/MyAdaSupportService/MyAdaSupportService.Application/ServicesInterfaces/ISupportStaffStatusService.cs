using MyAdaSupportService.Application.DTOs.Staff;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface ISupportStaffStatusService
{
    Task<SupportStaffStatusDto> CreateStatusAsync(CreateSupportStaffStatusDto dto);
    Task<SupportStaffStatusDto> GetOrCreateByMemberIdAsync(Guid memberId);
    Task<SupportStaffStatusDto?> GetByMemberIdAsync(Guid memberId);
    Task<List<SupportStaffStatusDto>> GetByStatusAsync(string status);
    Task UpdateStatusAsync(Guid memberId, UpdateSupportStaffStatusDto dto);
}
