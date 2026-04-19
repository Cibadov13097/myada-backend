using System.Threading;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IClubPositionRepository
{
    Task<ClubPosition?> GetByIdWithDetailsAsync(int id, int clubId, CancellationToken cancellationToken = default);
    Task<ClubPosition?> GetByIdWithDetailsNoTrackingAsync(int id, int clubId, CancellationToken cancellationToken = default);
    Task<List<ClubPosition>> ListByClubAsync(int clubId, CancellationToken cancellationToken = default);
    Task AddAsync(ClubPosition position, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClubPosition position, CancellationToken cancellationToken = default);
    Task ReplaceRequirementsAsync(int clubPositionId, IReadOnlyList<string> requirementTexts, CancellationToken cancellationToken = default);
    Task ClearVacancyPositionLinksAsync(int clubPositionId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(ClubPosition position, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
