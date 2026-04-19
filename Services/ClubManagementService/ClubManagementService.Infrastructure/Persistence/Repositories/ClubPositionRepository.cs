using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class ClubPositionRepository : IClubPositionRepository
{
    private readonly ClubManagementDbContext _db;

    public ClubPositionRepository(ClubManagementDbContext db) => _db = db;

    public async Task<ClubPosition?> GetByIdWithDetailsAsync(int id, int clubId, CancellationToken cancellationToken = default)
    {
        return await _db.ClubPositions
            .Include(p => p.Requirements)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClubId == clubId, cancellationToken);
    }

    public async Task<ClubPosition?> GetByIdWithDetailsNoTrackingAsync(int id, int clubId, CancellationToken cancellationToken = default)
    {
        return await _db.ClubPositions
            .AsNoTracking()
            .Include(p => p.Requirements)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClubId == clubId, cancellationToken);
    }

    public async Task<List<ClubPosition>> ListByClubAsync(int clubId, CancellationToken cancellationToken = default)
    {
        return await _db.ClubPositions
            .AsNoTracking()
            .Include(p => p.Requirements)
            .Include(p => p.Category)
            .Where(p => p.ClubId == clubId)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ClubPosition position, CancellationToken cancellationToken = default)
    {
        await _db.ClubPositions.AddAsync(position, cancellationToken);
    }

    public Task UpdateAsync(ClubPosition position, CancellationToken cancellationToken = default)
    {
        _db.ClubPositions.Update(position);
        return Task.CompletedTask;
    }

    public async Task ReplaceRequirementsAsync(int clubPositionId, IReadOnlyList<string> requirementTexts, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ClubPositionRequirements
            .Where(r => r.ClubPositionId == clubPositionId)
            .ToListAsync(cancellationToken);
        _db.ClubPositionRequirements.RemoveRange(existing);

        var order = 0;
        foreach (var text in requirementTexts.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            await _db.ClubPositionRequirements.AddAsync(new ClubPositionRequirement
            {
                ClubPositionId = clubPositionId,
                SortOrder = order++,
                Text = text.Trim()
            }, cancellationToken);
        }
    }

    public async Task ClearVacancyPositionLinksAsync(int clubPositionId, CancellationToken cancellationToken = default)
    {
        await _db.JobVacancies
            .Where(v => v.ClubPositionId == clubPositionId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.ClubPositionId, (int?)null), cancellationToken);
    }

    public async Task SoftDeleteAsync(ClubPosition position, CancellationToken cancellationToken = default)
    {
        var reqs = await _db.ClubPositionRequirements
            .IgnoreQueryFilters()
            .Where(r => r.ClubPositionId == position.Id)
            .ToListAsync(cancellationToken);
        _db.ClubPositionRequirements.RemoveRange(reqs);

        position.IsDeleted = true;
        position.DeletedAt = DateTime.UtcNow;
        _db.ClubPositions.Update(position);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _db.SaveChangesAsync(cancellationToken);
}
