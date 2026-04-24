using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LostFoundService.Infrastructure.Persistence.Repositories;

public class LostAndFoundRepository : ILostAndFoundRepository
{
    private readonly LostFoundDbContext _context;

    public LostAndFoundRepository(LostFoundDbContext context)
    {
        _context = context;
    }

    private IQueryable<LostFoundItem> ItemsWithImages(bool asNoTracking)
    {
        var query = _context.LostFoundItems.Include(x => x.Images).AsQueryable();
        return asNoTracking ? query.AsNoTracking() : query;
    }

    public async Task<(List<LostFoundItem> Items, int Total)> SearchItemsAsync(
        string? searchTerm,
        LostFoundItemType? type,
        string? postedByUserId,
        string? category,
        LostFoundPublicStatus? status,
        LostFoundAdminStatus? adminStatus,
        int page,
        int limit)
    {
        var query = ItemsWithImages(asNoTracking: true);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(postedByUserId))
        {
            query = query.Where(x => x.PostedByUserId == postedByUserId);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            query = query.Where(x => x.Category.ToLower() == normalizedCategory);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (adminStatus.HasValue)
        {
            query = query.Where(x => x.AdminStatus == adminStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, pattern) ||
                EF.Functions.ILike(x.Location ?? string.Empty, pattern) ||
                EF.Functions.ILike(x.Description ?? string.Empty, pattern) ||
                EF.Functions.ILike(x.Category, pattern));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.PostedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<LostFoundItem?> GetItemByIdAsync(int id, bool asNoTracking = false, bool includeImages = true)
    {
        var query = includeImages
            ? ItemsWithImages(asNoTracking)
            : (asNoTracking ? _context.LostFoundItems.AsNoTracking() : _context.LostFoundItems.AsQueryable());

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<LostFoundItem?> GetTrackedItemAsync(int id, bool includeImages = false)
    {
        var query = includeImages
            ? ItemsWithImages(asNoTracking: false)
            : _context.LostFoundItems.AsQueryable();

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<int> CountItemsAsync()
    {
        return _context.LostFoundItems.CountAsync();
    }

    public Task<int> CountItemsByStatusAsync(LostFoundPublicStatus status)
    {
        return _context.LostFoundItems.CountAsync(x => x.Status == status);
    }

    public async Task AddItemAsync(LostFoundItem item)
    {
        await _context.LostFoundItems.AddAsync(item);
    }

    public async Task AssignUniqueReferenceNumberAsync(LostFoundItem item)
    {
        const int maxAttempts = 100;
        var nextSequence = await GetNextReferenceSequenceAsync();

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            item.ReferenceNumber = FormatReferenceNumber(nextSequence + attempt);

            try
            {
                await _context.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException ex) when (IsReferenceNumberUniqueViolation(ex))
            {
                // Try the next available number if another row already owns this reference.
            }
        }

        throw new InvalidOperationException("Unable to generate a unique lost and found reference number.");
    }

    public async Task AddItemImagesAsync(IEnumerable<LostFoundItemImage> images)
    {
        await _context.LostFoundItemImages.AddRangeAsync(images);
    }

    public Task<bool> ItemExistsAsync(int id)
    {
        return _context.LostFoundItems.AnyAsync(x => x.Id == id);
    }

    public Task<bool> ClaimExistsAsync(int itemId, string userId, LostFoundClaimType claimType)
    {
        return _context.LostFoundItemClaims.AnyAsync(x =>
            x.LostFoundItemId == itemId &&
            x.UserId == userId &&
            x.ClaimType == claimType);
    }

    public async Task AddClaimAsync(LostFoundItemClaim claim)
    {
        await _context.LostFoundItemClaims.AddAsync(claim);
    }

    public Task<List<LostFoundItemClaim>> GetClaimsByItemIdAsync(int id)
    {
        return _context.LostFoundItemClaims
            .Where(x => x.LostFoundItemId == id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    private async Task<int> GetNextReferenceSequenceAsync()
    {
        var latestReferenceNumber = await _context.LostFoundItems
            .AsNoTracking()
            .Where(x => x.ReferenceNumber != null && x.ReferenceNumber.StartsWith("LF-"))
            .OrderByDescending(x => x.ReferenceNumber)
            .Select(x => x.ReferenceNumber)
            .FirstOrDefaultAsync();

        if (!TryParseReferenceSequence(latestReferenceNumber, out var sequence))
        {
            return 1;
        }

        return sequence + 1;
    }

    private static string FormatReferenceNumber(int sequence)
    {
        return $"LF-{sequence:D6}";
    }

    private static bool TryParseReferenceSequence(string? referenceNumber, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(referenceNumber) || !referenceNumber.StartsWith("LF-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(referenceNumber["LF-".Length..], out sequence);
    }

    private static bool IsReferenceNumberUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               string.Equals(
                   postgresException.ConstraintName,
                   "IX_LostFoundItems_ReferenceNumber",
                   StringComparison.Ordinal);
    }
}
