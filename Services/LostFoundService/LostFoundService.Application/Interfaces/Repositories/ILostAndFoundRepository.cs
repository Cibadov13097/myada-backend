using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;

namespace LostFoundService.Application.Interfaces.Repositories;

public interface ILostAndFoundRepository
{
    Task<(List<LostFoundItem> Items, int Total)> SearchItemsAsync(
        string? searchTerm,
        LostFoundItemType? type,
        string? postedByUserId,
        int? categoryId,
        LostFoundPublicStatus? status,
        LostFoundAdminStatus? adminStatus,
        int page,
        int limit);

    Task<LostFoundItem?> GetItemByIdAsync(int id, bool asNoTracking = false, bool includeImages = true);
    Task<LostFoundItem?> GetTrackedItemAsync(int id, bool includeImages = false);
    Task<int> CountItemsAsync();
    Task<int> CountItemsByStatusAsync(LostFoundPublicStatus status);
    Task<bool> CategoryExistsAsync(int categoryId);
    Task AddItemAsync(LostFoundItem item);
    Task AssignUniqueReferenceNumberAsync(LostFoundItem item);
    Task AddItemImagesAsync(IEnumerable<LostFoundItemImage> images);
    Task<bool> ItemExistsAsync(int id);
    Task<bool> ClaimExistsAsync(int itemId, string userId, LostFoundClaimType claimType);
    Task AddClaimAsync(LostFoundItemClaim claim);
    Task<List<LostFoundItemClaim>> GetClaimsByItemIdAsync(int id);
    Task SaveChangesAsync();
}
