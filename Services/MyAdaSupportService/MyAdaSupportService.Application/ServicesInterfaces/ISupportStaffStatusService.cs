using MyAdaSupportService.Application.DTOs.Staff;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface ISupportStaffStatusService
{
    Task<SupportStaffStatusDto> CreateStatusAsync(CreateSupportStaffStatusDto dto);
    Task<SupportStaffStatusDto?> GetByMemberIdAsync(int memberId);
    Task<List<SupportStaffStatusDto>> GetByStatusAsync(string status);
    Task UpdateStatusAsync(int memberId, UpdateSupportStaffStatusDto dto);
}
