using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Infrastructure.Repositories;

public class SupportStaffStatusRepository
    : EfCoreRepository<SupportStaffStatus>, ISupportStaffStatusRepository
{
    public SupportStaffStatusRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SupportStaffStatus?> GetByMemberIdAsync(int memberId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MemberId == memberId);
    }

    public async Task<List<SupportStaffStatus>> GetByStatusAsync(string status)
    {
        if (!Enum.TryParse<StaffAvailabilityStatus>(status, true, out var parsedStatus))
            return new List<SupportStaffStatus>();

        return await _dbSet
            .Where(s => s.Status == parsedStatus)
            .AsNoTracking()
            .ToListAsync();
    }
}