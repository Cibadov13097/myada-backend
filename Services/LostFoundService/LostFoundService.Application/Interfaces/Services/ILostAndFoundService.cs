using LostFoundService.Application.DTOs.LostAndFound;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Interfaces.Services;

public interface ILostAndFoundService
{
    Task<LostFoundItemListResult> GetItemsAsync(LostFoundItemSearchQuery query);
    Task<LostFoundItem> GetItemByIdAsync(int id, bool hideTerminalItemsForPublic = false);
    Task<LostFoundStats> GetStatsAsync();
    Task<LostFoundItem> CreateItemAsync(CreateLostFoundItemCommand command);
    Task<LostFoundItem> UpdateItemAsync(int id, UpdateLostFoundItemCommand command);
    Task NotifyOwnerAsync(int id, NotifyLostFoundOwnerCommand command);
    Task<LostFoundItemClaim> CreateClaimAsync(int id, CreateLostFoundClaimCommand command);
    Task<LostFoundItem> ConfirmReceiptAsync(int id, ConfirmLostFoundReceiptCommand command);
    Task<LostFoundItem> CompleteDeliveryAsync(int id, CompleteLostFoundDeliveryCommand command);
    Task<List<LostFoundItemClaim>> GetItemClaimsAsync(int id);
    Task<LostFoundItem> GetTimelineItemAsync(int id, bool hideTerminalItemsForPublic = false);
}
