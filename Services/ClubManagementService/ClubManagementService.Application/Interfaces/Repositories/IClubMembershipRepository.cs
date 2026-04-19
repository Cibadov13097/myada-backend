using System.Threading;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IClubMembershipRepository : IGenericRepository<ClubMembership>
{
    Task<List<int>> GetClubIdsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
