using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.Abstractions;

public interface ISupportStaffStatusRepository : IRepository<SupportStaffStatus>
{
    Task<SupportStaffStatus?> GetByMemberIdAsync(Guid memberId);
    Task<List<SupportStaffStatus>> GetByStatusAsync(string status);
}
