using ClubManagementService.Application.DTOs.Club;

namespace ClubManagementService.Application.Interfaces.Services;

public interface IClubService
{
    Task<ClubDto?> GetByIdAsync(int id);
    Task<List<ClubDto>> GetAllAsync();
    Task<List<ClubDto>> GetByCategoryIdAsync(int categoryId);
    Task<ClubDto> CreateAsync(CreateClubDto dto);
    Task<ClubDto> UpdateAsync(int id, UpdateClubDto dto);
    Task DeleteAsync(int id);
}
