using LostFoundService.Application.DTOs.LostAndFound;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Services;

public class LostAndFoundService : ILostAndFoundService
{
    private readonly ILostAndFoundRepository _repository;

    public LostAndFoundService(ILostAndFoundRepository repository)
    {
        _repository = repository;
    }

    public async Task<LostFoundItemListResult> GetItemsAsync(LostFoundItemSearchQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var effectiveQuery = string.IsNullOrWhiteSpace(query.Q) ? query.Search : query.Q;

        LostFoundItemType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(query.Type) &&
            !query.Type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseItemType(query.Type, out var itemType))
            {
                throw ValidationError("VALIDATION_ERROR", "type must be lost, found, or all.");
            }

            parsedType = itemType;
        }

        string? postedByUserId = null;
        if (string.Equals(query.PostedBy, "me", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(query.CurrentUserId))
            {
                throw new UnauthorizedException("Login required for postedBy=me.");
            }

            postedByUserId = query.CurrentUserId;
        }

        LostFoundPublicStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!TryParsePublicStatus(query.Status, out var status))
            {
                throw ValidationError("VALIDATION_ERROR", "status must be active or pending.");
            }

            parsedStatus = status;
        }

        LostFoundAdminStatus? parsedAdminStatus = null;
        if (!string.IsNullOrWhiteSpace(query.AdminStatus))
        {
            if (!TryParseAdminStatus(query.AdminStatus, out var adminStatus))
            {
                throw ValidationError("VALIDATION_ERROR", "adminStatus must be Pending, Received, or Delivered.");
            }

            parsedAdminStatus = adminStatus;
        }

        var result = await _repository.SearchItemsAsync(
            effectiveQuery,
            parsedType,
            postedByUserId,
            query.CategoryId,
            parsedStatus,
            parsedAdminStatus,
            query.ExcludeTerminalAdminItems,
            page,
            limit,
            query.CurrentUserId);

        return new LostFoundItemListResult
        {
            Items = result.Items,
            Total = result.Total
        };
    }

    public async Task<LostFoundItem> GetItemByIdAsync(int id, string? currentUserId = null, bool hideTerminalItemsForPublic = false)
    {
        var item = await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: true, currentUserId: currentUserId);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (hideTerminalItemsForPublic && IsTerminalAdminStatus(item.AdminStatus))
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        return item;
    }

    public async Task<LostFoundStats> GetStatsAsync()
    {
        return new LostFoundStats
        {
            Total = await _repository.CountCatalogItemsExcludingTerminalAsync(publicStatus: null),
            Active = await _repository.CountCatalogItemsExcludingTerminalAsync(LostFoundPublicStatus.Active),
            PendingVerification = await _repository.CountCatalogItemsExcludingTerminalAsync(LostFoundPublicStatus.PendingVerification)
        };
    }

    public async Task<LostFoundItem> CreateItemAsync(CreateLostFoundItemCommand command)
    {
        if (!TryParseItemType(command.Type, out var itemType))
        {
            throw ValidationError("VALIDATION_ERROR", "type must be lost or found.");
        }

        ValidateCreateCommand(command, itemType);
        if (!await _repository.CategoryExistsAsync(command.CategoryId!.Value))
        {
            throw ValidationError("VALIDATION_ERROR", "categoryId must reference an existing category.");
        }

        var imageUrls = NormalizeDistinctImageUrls(command.ImageUrls);
        var postedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(command.DateLost) &&
            DateOnly.TryParse(command.DateLost, out var dateOnly))
        {
            postedAt = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        var item = new LostFoundItem
        {
            Type = itemType,
            Title = command.ItemName!.Trim(),
            CategoryId = command.CategoryId.Value,
            Description = command.Description?.Trim(),
            LocationType = command.LocationType?.Trim(),
            BuildingCode = command.BuildingCode?.Trim(),
            IsRoom = command.IsRoom?.Trim(),
            RoomOrArea = command.RoomOrArea?.Trim(),
            CampusLocation = command.CampusLocation?.Trim(),
            Building = command.Building?.Trim(),
            Floor = command.Floor?.Trim(),
            RoomArea = command.RoomArea?.Trim(),
            LastKnownLocation = command.LastKnownLocation?.Trim(),
            TimeLost = command.TimeLost?.Trim(),
            ContactName = command.ContactName?.Trim(),
            ContactPhone = command.ContactPhone?.Trim(),
            CollectionPlace = command.CollectionPlace?.Trim(),
            PostedAt = postedAt,
            PostedByUserId = command.PostedByUserId,
            Status = TryParsePublicStatus(command.Status, out var parsedStatus)
                ? parsedStatus
                : LostFoundPublicStatus.PendingVerification,
            AdminStatus = TryParseAdminStatus(command.AdminStatus, out var parsedAdminStatus)
                ? parsedAdminStatus
                : LostFoundAdminStatus.Pending,
            StorageBinId = command.StorageBinId?.Trim(),
            Condition = TryParseCondition(command.Condition, out var parsedCondition) ? parsedCondition : null,
            AdminNotes = command.AdminNotes,
            ReportedColor = command.ReportedColor,
            IntakePhotoUrl = command.IntakePhotoUrl,
            ConfirmAccuracy = command.ConfirmAccuracy,
            ClaimantName = command.ClaimantName,
            ClaimantStudentId = command.ClaimantStudentId,
            StudentIdVerified = command.StudentIdVerified,
            HandoverProofUrl = command.HandoverProofUrl,
            CreatedByUserId = command.CreatedByUserId
        };

        if (!command.IsAdminCreate)
        {
            item.Status = LostFoundPublicStatus.PendingVerification;
            item.AdminStatus = LostFoundAdminStatus.Pending;
        }

        item.Location = string.IsNullOrWhiteSpace(command.Location)
            ? BuildLocation(item)
            : command.Location.Trim();

        item.PrimaryImageUrl = imageUrls.FirstOrDefault();
        if (DateOnly.TryParse(command.DateLost, out var parsedDate))
        {
            item.DateLost = parsedDate;
        }

        await _repository.AddItemAsync(item);
        await _repository.SaveChangesAsync();

        await _repository.AssignUniqueReferenceNumberAsync(item);
        item.Images = imageUrls.Select((url, index) => new LostFoundItemImage
        {
            LostFoundItemId = item.Id,
            Url = url,
            SortOrder = index
        }).ToList();

        if (item.Images.Count > 0)
        {
            await _repository.AddItemImagesAsync(item.Images);
        }

        await _repository.SaveChangesAsync();
        return await _repository.GetItemByIdAsync(item.Id, asNoTracking: true, includeImages: true) ?? item;
    }

    public async Task<LostFoundItem> UpdateItemAsync(int id, UpdateLostFoundItemCommand command)
    {
        var item = await _repository.GetTrackedItemAsync(id, includeImages: true);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (!string.IsNullOrWhiteSpace(command.AdminStatus))
        {
            if (!TryParseAdminStatus(command.AdminStatus, out var parsedAdminStatus))
            {
                throw ValidationError("VALIDATION_ERROR", "adminStatus must be Pending, Received, or Delivered.");
            }

            item.AdminStatus = parsedAdminStatus;
        }

        if (command.CategoryId.HasValue)
        {
            if (!await _repository.CategoryExistsAsync(command.CategoryId.Value))
            {
                throw ValidationError("VALIDATION_ERROR", "categoryId must reference an existing category.");
            }

            item.CategoryId = command.CategoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(command.Title))
        {
            item.Title = command.Title.Trim();
        }

        if (command.Description != null)
        {
            item.Description = command.Description;
        }

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            if (!TryParsePublicStatus(command.Status, out var parsedStatus))
            {
                throw ValidationError("VALIDATION_ERROR", "status must be Active or Pending Verification.");
            }

            item.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(command.StorageBinId))
        {
            item.StorageBinId = command.StorageBinId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.Condition))
        {
            if (!TryParseCondition(command.Condition, out var parsedCondition))
            {
                throw ValidationError("VALIDATION_ERROR", "condition must be good, fair, or damaged.");
            }

            item.Condition = parsedCondition;
        }

        if (command.AdminNotes != null)
        {
            item.AdminNotes = command.AdminNotes;
        }

        if (command.ReportedColor != null)
        {
            item.ReportedColor = command.ReportedColor;
        }

        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = command.ModifiedByUserId;

        await _repository.SaveChangesAsync();
        return await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: true) ?? item;
    }

    public async Task NotifyOwnerAsync(int id, NotifyLostFoundOwnerCommand command)
    {
        var item = await _repository.GetTrackedItemAsync(id);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        item.LastNotifiedAt = DateTime.UtcNow;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = command.ModifiedByUserId;

        await _repository.SaveChangesAsync();
    }

    public async Task<LostFoundItemClaim> CreateClaimAsync(int id, CreateLostFoundClaimCommand command)
    {
        var item = await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: false);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (IsTerminalAdminStatus(item.AdminStatus))
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (!TryParseClaimType(command.ClaimType, out var claimType))
        {
            throw ValidationError("VALIDATION_ERROR", "claimType must be owner or finder.");
        }

        var exists = await _repository.ClaimExistsAsync(id, command.UserId, claimType);
        if (exists)
        {
            throw new ConflictException("DUPLICATE_CLAIM", "You have already submitted this claim type for this item.");
        }

        var claim = new LostFoundItemClaim
        {
            LostFoundItemId = id,
            UserId = command.UserId,
            ClaimType = claimType,
            Message = command.Message?.Trim()
        };

        await _repository.AddClaimAsync(claim);
        await _repository.SaveChangesAsync();
        return claim;
    }

    public async Task<LostFoundItem> ConfirmReceiptAsync(int id, ConfirmLostFoundReceiptCommand command)
    {
        var item = await _repository.GetTrackedItemAsync(id, includeImages: true);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (!TryParseCondition(command.Condition, out var parsedCondition))
        {
            throw ValidationError("VALIDATION_ERROR", "condition must be good, fair, or damaged.");
        }

        item.AdminStatus = LostFoundAdminStatus.Received;
        item.Status = LostFoundPublicStatus.Active;
        item.StorageBinId = command.StorageBinId;
        item.Condition = parsedCondition;
        item.IntakePhotoUrl = command.IntakePhotoUrl ?? item.IntakePhotoUrl;
        item.ConfirmAccuracy = command.ConfirmAccuracy;
        item.AdminNotes = command.AdminNotes;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = command.ModifiedByUserId;

        await _repository.SaveChangesAsync();
        return await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: true) ?? item;
    }

    public async Task<LostFoundItem> CompleteDeliveryAsync(int id, CompleteLostFoundDeliveryCommand command)
    {
        var item = await _repository.GetTrackedItemAsync(id, includeImages: true);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        item.AdminStatus = LostFoundAdminStatus.Delivered;
        item.ClaimantName = command.ClaimantName;
        item.ClaimantStudentId = command.ClaimantStudentId;
        item.StudentIdVerified = command.StudentIdVerified;
        item.HandoverProofUrl = command.HandoverProofUrl ?? item.HandoverProofUrl;
        item.DeliveryCompletedAt = DateTime.UtcNow;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = command.ModifiedByUserId;

        await _repository.SaveChangesAsync();
        return await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: true) ?? item;
    }

    public async Task<List<LostFoundItemClaim>> GetItemClaimsAsync(int id)
    {
        var itemExists = await _repository.ItemExistsAsync(id);
        if (!itemExists)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        return await _repository.GetClaimsByItemIdAsync(id);
    }

    public async Task<LostFoundItem> GetTimelineItemAsync(int id, bool hideTerminalItemsForPublic = false)
    {
        var item = await _repository.GetItemByIdAsync(id, asNoTracking: true, includeImages: false);
        if (item == null)
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        if (hideTerminalItemsForPublic && IsTerminalAdminStatus(item.AdminStatus))
        {
            throw new NotFoundException($"Item {id} was not found.");
        }

        return item;
    }

    private static bool IsTerminalAdminStatus(LostFoundAdminStatus? adminStatus) =>
        adminStatus is LostFoundAdminStatus.Received or LostFoundAdminStatus.Delivered;

    private static void ValidateCreateCommand(CreateLostFoundItemCommand command, LostFoundItemType type)
    {
        var details = new Dictionary<string, List<string>>();

        void Add(string key, string message)
        {
            if (!details.ContainsKey(key))
            {
                details[key] = new List<string>();
            }

            details[key].Add(message);
        }

        if (string.IsNullOrWhiteSpace(command.ItemName)) Add("itemName", "itemName is required.");
        if (!command.CategoryId.HasValue) Add("categoryId", "categoryId is required.");
        else if (command.CategoryId.Value <= 0) Add("categoryId", "categoryId must be a positive integer.");
        if (!string.IsNullOrWhiteSpace(command.Description) && command.Description.Length > 500) Add("description", "description max length is 500.");
        if (!string.IsNullOrWhiteSpace(command.Location) && command.Location.Length > 400) Add("location", "location max length is 400.");

        if (!string.IsNullOrWhiteSpace(command.Status) && !TryParsePublicStatus(command.Status, out _))
            Add("status", "status must be Active or Pending Verification.");

        if (!string.IsNullOrWhiteSpace(command.AdminStatus) && !TryParseAdminStatus(command.AdminStatus, out _))
            Add("adminStatus", "adminStatus must be Pending, Received, or Delivered.");

        if (!string.IsNullOrWhiteSpace(command.Condition) && !TryParseCondition(command.Condition, out _))
            Add("condition", "condition must be good, fair, or damaged.");

        foreach (var imageUrl in command.ImageUrls)
        {
            if (imageUrl.Length > 500)
            {
                Add("imageUrls", "image URL max length is 500.");
                continue;
            }

            if (!IsHttpUrl(imageUrl))
            {
                Add("imageUrls", "image URLs must be absolute http/https URLs.");
            }
        }

        if (type == LostFoundItemType.Found)
        {
            if (string.IsNullOrWhiteSpace(command.LocationType)) Add("locationType", "locationType is required for found reports.");

            if (string.Equals(command.LocationType, "building", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(command.BuildingCode) && string.IsNullOrWhiteSpace(command.Building))
                {
                    Add("buildingCode", "buildingCode/building is required when locationType=building.");
                }

                if (string.IsNullOrWhiteSpace(command.RoomOrArea) && string.IsNullOrWhiteSpace(command.RoomArea))
                {
                    Add("roomOrArea", "roomOrArea/roomArea is required when locationType=building.");
                }
            }

            if (string.Equals(command.LocationType, "campus", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(command.CampusLocation))
            {
                Add("campusLocation", "campusLocation is required when locationType=campus.");
            }

            if (string.Equals(command.LocationType, "other", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(command.Location))
            {
                Add("location", "location is required when locationType=other.");
            }

            if (command.ImageUrls.Count == 0) Add("photos", "At least one photo is required for found reports.");
            if (string.IsNullOrWhiteSpace(command.CollectionPlace)) Add("collectionPlace", "collectionPlace is required for found reports.");
        }

        if (command.IsAdminCreate && !string.IsNullOrWhiteSpace(command.Type) &&
            !TryParseItemType(command.Type, out _))
        {
            Add("type", "type must be lost or found.");
        }

        if (details.Count > 0)
        {
            throw ValidationError("VALIDATION_ERROR", "Validation failed.", details);
        }
    }

    private static string? BuildLocation(LostFoundItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.BuildingCode)) parts.Add(item.BuildingCode);
        else if (!string.IsNullOrWhiteSpace(item.Building)) parts.Add(item.Building);

        if (!string.IsNullOrWhiteSpace(item.RoomOrArea)) parts.Add(item.RoomOrArea);
        else if (!string.IsNullOrWhiteSpace(item.RoomArea)) parts.Add(item.RoomArea);

        if (!string.IsNullOrWhiteSpace(item.CampusLocation)) parts.Add(item.CampusLocation);
        if (!string.IsNullOrWhiteSpace(item.LastKnownLocation)) parts.Add(item.LastKnownLocation);

        var location = string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(location) ? null : location;
    }

    private static List<string> NormalizeDistinctImageUrls(IEnumerable<string> imageUrls)
    {
        return imageUrls
            .Select(url => url.Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool TryParseItemType(string? value, out LostFoundItemType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "lost":
                type = LostFoundItemType.Lost;
                return true;
            case "found":
                type = LostFoundItemType.Found;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParsePublicStatus(string? value, out LostFoundPublicStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "active":
                status = LostFoundPublicStatus.Active;
                return true;
            case "pending":
            case "pending verification":
            case "pending_verification":
                status = LostFoundPublicStatus.PendingVerification;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseAdminStatus(string? value, out LostFoundAdminStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "pending":
                status = LostFoundAdminStatus.Pending;
                return true;
            case "received":
                status = LostFoundAdminStatus.Received;
                return true;
            case "delivered":
                status = LostFoundAdminStatus.Delivered;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseCondition(string? value, out LostFoundItemCondition condition)
    {
        condition = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "good":
                condition = LostFoundItemCondition.Good;
                return true;
            case "fair":
                condition = LostFoundItemCondition.Fair;
                return true;
            case "damaged":
                condition = LostFoundItemCondition.Damaged;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseClaimType(string? value, out LostFoundClaimType claimType)
    {
        claimType = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        switch (value.Trim().ToLowerInvariant())
        {
            case "owner":
                claimType = LostFoundClaimType.Owner;
                return true;
            case "finder":
                claimType = LostFoundClaimType.Finder;
                return true;
            default:
                return false;
        }
    }

    private static BadRequestException ValidationError(
        string errorCode,
        string message,
        Dictionary<string, List<string>>? details = null)
    {
        return new BadRequestException(errorCode, message, details);
    }
}
