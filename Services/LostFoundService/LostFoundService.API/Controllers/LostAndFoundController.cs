using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using LostFoundService.Application.Extensions;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Api.Controllers;

[ApiController]
[Route("api/lost-and-found")]
public class LostAndFoundController : ControllerBase
{
    private static readonly HashSet<string> CanonicalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Electronics",
        "Documents",
        "Personal Items",
        "Accessories"
    };

    private readonly LostFoundDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public LostAndFoundController(LostFoundDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery(Name = "q")] string? q,
        [FromQuery(Name = "search")] string? search,
        [FromQuery] string? type,
        [FromQuery] string? postedBy,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? adminStatus,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        var effectiveQuery = string.IsNullOrWhiteSpace(q) ? search : q;
        var query = _dbContext.LostFoundItems
            .AsNoTracking()
            .Include(x => x.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseItemType(type, out var parsedType))
            {
                return BadRequest(Error("VALIDATION_ERROR", "type must be lost, found, or all."));
            }

            query = query.Where(x => x.Type == parsedType);
        }

        var userId = TryGetUserId();
        if (string.Equals(postedBy, "me", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(Error("UNAUTHORIZED", "Login required for postedBy=me."));
            }

            query = query.Where(x => x.PostedByUserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All Items", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = NormalizeCategory(category);
            query = query.Where(x => x.Category == normalized);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParsePublicStatus(status, out var parsedStatus))
            {
                return BadRequest(Error("VALIDATION_ERROR", "status must be active or pending."));
            }

            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(adminStatus))
        {
            if (!TryParseAdminStatus(adminStatus, out var parsedAdminStatus))
            {
                return BadRequest(Error("VALIDATION_ERROR", "adminStatus must be Pending, Received, or Delivered."));
            }

            query = query.Where(x => x.AdminStatus == parsedAdminStatus);
        }

        if (!string.IsNullOrWhiteSpace(effectiveQuery))
        {
            var pattern = $"%{effectiveQuery.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, pattern) ||
                EF.Functions.ILike(x.Location ?? string.Empty, pattern) ||
                EF.Functions.ILike(x.Description ?? string.Empty, pattern) ||
                EF.Functions.ILike(x.Category, pattern));
        }

        var total = await query.CountAsync();
        var isAdmin = IsAdmin();
        var items = await query
            .OrderByDescending(x => x.PostedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(new
        {
            items = items.Select(x => ToItemResponse(x, isAdmin)),
            total
        });
    }

    [HttpGet("items/{id:int}")]
    public async Task<IActionResult> GetItemById(int id)
    {
        var item = await _dbContext.LostFoundItems
            .AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        return Ok(ToItemResponse(item, IsAdmin()));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var total = await _dbContext.LostFoundItems.CountAsync();
        var active = await _dbContext.LostFoundItems.CountAsync(x => x.Status == LostFoundPublicStatus.Active);
        var pending = await _dbContext.LostFoundItems.CountAsync(x => x.Status == LostFoundPublicStatus.PendingVerification);

        return Ok(new
        {
            total,
            active,
            pendingVerification = pending
        });
    }

    [HttpPost("reports/lost")]
    public async Task<IActionResult> CreateLostReport()
    {
        var model = await ReadReportRequestAsync();
        if (model == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "Invalid request payload."));
        }

        var validation = ValidateReport(model, LostFoundItemType.Lost);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        var item = await CreateItemAsync(model, LostFoundItemType.Lost, isAdminCreate: false);
        return StatusCode(StatusCodes.Status201Created, new
        {
            id = item.Id,
            referenceNumber = item.ReferenceNumber,
            status = ToPublicStatusString(item.Status),
            message = "Lost item report submitted for review."
        });
    }

    [HttpPost("reports/found")]
    public async Task<IActionResult> CreateFoundReport()
    {
        var model = await ReadReportRequestAsync();
        if (model == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "Invalid request payload."));
        }

        var validation = ValidateReport(model, LostFoundItemType.Found);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        var item = await CreateItemAsync(model, LostFoundItemType.Found, isAdminCreate: false);
        return StatusCode(StatusCodes.Status201Created, new
        {
            id = item.Id,
            referenceNumber = item.ReferenceNumber,
            status = ToPublicStatusString(item.Status),
            message = "Found item report submitted for review."
        });
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPatch("items/{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] PatchItemRequest request)
    {
        var item = await _dbContext.LostFoundItems.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        if (!string.IsNullOrWhiteSpace(request.AdminStatus))
        {
            if (!TryParseAdminStatus(request.AdminStatus, out var parsedAdminStatus))
            {
                return BadRequest(Error("VALIDATION_ERROR", "adminStatus must be Pending, Received, or Delivered."));
            }

            item.AdminStatus = parsedAdminStatus;
        }
        if (!string.IsNullOrWhiteSpace(request.Title)) item.Title = request.Title;
        if (request.Description != null) item.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParsePublicStatus(request.Status, out var parsedStatus))
            {
                return BadRequest(Error("VALIDATION_ERROR", "status must be Active or Pending Verification."));
            }

            item.Status = parsedStatus;
        }
        if (!string.IsNullOrWhiteSpace(request.StorageBinId)) item.StorageBinId = request.StorageBinId;
        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            if (!TryParseCondition(request.Condition, out var parsedCondition))
            {
                return BadRequest(Error("VALIDATION_ERROR", "condition must be good, fair, or damaged."));
            }

            item.Condition = parsedCondition;
        }
        if (request.AdminNotes != null) item.AdminNotes = request.AdminNotes;
        if (request.ReportedColor != null) item.ReportedColor = request.ReportedColor;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = TryGetUserId();

        await _dbContext.SaveChangesAsync();
        return Ok(ToItemResponse(item, true));
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items")]
    public async Task<IActionResult> CreateManualItem()
    {
        var model = await ReadReportRequestAsync();
        if (model == null || string.IsNullOrWhiteSpace(model.Type))
        {
            return BadRequest(Error("VALIDATION_ERROR", "type is required for manual create."));
        }

        if (!TryParseItemType(model.Type, out var itemType))
        {
            return BadRequest(Error("VALIDATION_ERROR", "type must be lost or found."));
        }

        var validation = ValidateReport(model, itemType, isAdminCreate: true);
        if (validation != null)
        {
            return BadRequest(validation);
        }

        var item = await CreateItemAsync(model, itemType, isAdminCreate: true);
        return StatusCode(StatusCodes.Status201Created, ToItemResponse(item, true));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile? file, IFormFile? photo)
    {
        var upload = file ?? photo;
        if (upload == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "file or photo is required."));
        }

        var url = await SaveFileAsync(upload);
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(Error("VALIDATION_ERROR", "Only PNG/JPG files are supported."));
        }
        return StatusCode(StatusCodes.Status201Created, new { url });
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/notify")]
    public async Task<IActionResult> NotifyOwner(int id, [FromBody] NotifyRequest request)
    {
        var item = await _dbContext.LostFoundItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        item.LastNotifiedAt = DateTime.UtcNow;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = TryGetUserId();
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status202Accepted, new { queued = true });
    }

    [Authorize]
    [HttpPost("items/{id:int}/claims")]
    public async Task<IActionResult> CreateClaim(int id, [FromBody] CreateClaimRequest request)
    {
        var item = await _dbContext.LostFoundItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        var userId = User.GetUserId();
        if (!TryParseClaimType(request.ClaimType, out var claimType))
        {
            return BadRequest(Error("VALIDATION_ERROR", "claimType must be owner or finder."));
        }

        var exists = await _dbContext.LostFoundItemClaims.AnyAsync(x =>
            x.LostFoundItemId == id && x.UserId == userId && x.ClaimType == claimType);
        if (exists)
        {
            return Conflict(Error("DUPLICATE_CLAIM", "You have already submitted this claim type for this item."));
        }

        var claim = new LostFoundItemClaim
        {
            LostFoundItemId = id,
            UserId = userId,
            ClaimType = claimType,
            Message = request.Message?.Trim()
        };

        _dbContext.LostFoundItemClaims.Add(claim);
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { claimId = claim.Id, status = ToClaimStatusString(claim.Status) });
    }
    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(int id, [FromBody] ConfirmReceiptRequest request)
    {
        var item = await _dbContext.LostFoundItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        if (!TryParseCondition(request.Condition, out var parsedCondition))
        {
            return BadRequest(Error("VALIDATION_ERROR", "condition must be good, fair, or damaged."));
        }

        item.AdminStatus = LostFoundAdminStatus.Received;
        item.StorageBinId = request.StorageBinId;
        item.Condition = parsedCondition;
        item.IntakePhotoUrl = request.IntakePhotoUrl;
        item.ConfirmAccuracy = request.ConfirmAccuracy;
        item.AdminNotes = request.AdminNotes;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = TryGetUserId();

        await _dbContext.SaveChangesAsync();
        return Ok(ToItemResponse(item, true));
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/complete-delivery")]
    public async Task<IActionResult> CompleteDelivery(int id, [FromBody] CompleteDeliveryRequest request)
    {
        var item = await _dbContext.LostFoundItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        item.AdminStatus = LostFoundAdminStatus.Delivered;
        item.ClaimantName = request.ClaimantName;
        item.ClaimantStudentId = request.ClaimantStudentId;
        item.StudentIdVerified = request.StudentIdVerified;
        item.HandoverProofUrl = request.HandoverProofUrl;
        item.DeliveryCompletedAt = DateTime.UtcNow;
        item.ModifiedAt = DateTime.UtcNow;
        item.ModifiedByUserId = TryGetUserId();

        await _dbContext.SaveChangesAsync();
        return Ok(ToItemResponse(item, true));
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpGet("items/{id:int}/claims")]
    public async Task<IActionResult> GetItemClaims(int id)
    {
        var itemExists = await _dbContext.LostFoundItems.AnyAsync(x => x.Id == id);
        if (!itemExists)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        var claimRows = await _dbContext.LostFoundItemClaims
            .Where(x => x.LostFoundItemId == id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var claims = claimRows.Select(x => new
        {
            x.Id,
            claimType = ToClaimTypeString(x.ClaimType),
            x.Message,
            status = ToClaimStatusString(x.Status),
            x.UserId,
            x.CreatedAt
        });

        return Ok(new { claims });
    }

    [HttpGet("items/{id:int}/timeline")]
    public async Task<IActionResult> GetTimeline(int id)
    {
        var item = await _dbContext.LostFoundItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound(Error("NOT_FOUND", $"Item {id} was not found."));
        }

        var events = new List<object>
        {
            new { at = item.PostedAt, label = "Report submitted", done = true },
            new { at = item.LastNotifiedAt, label = "Owner notified", done = item.LastNotifiedAt.HasValue },
            new { at = item.DeliveryCompletedAt, label = "Delivery completed", done = item.DeliveryCompletedAt.HasValue }
        };

        return Ok(new { events });
    }

    private async Task<ReportRequest?> ReadReportRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var model = new ReportRequest
            {
                Type = GetForm(form, "type"),
                ItemName = GetForm(form, "itemName"),
                Category = GetForm(form, "category"),
                Description = GetForm(form, "description"),
                LocationType = GetForm(form, "locationType"),
                BuildingCode = GetForm(form, "buildingCode"),
                IsRoom = GetForm(form, "isRoom"),
                RoomOrArea = GetForm(form, "roomOrArea"),
                CampusLocation = GetForm(form, "campusLocation"),
                Building = GetForm(form, "building"),
                Floor = GetForm(form, "floor"),
                RoomArea = GetForm(form, "roomArea"),
                LastKnownLocation = GetForm(form, "lastKnownLocation"),
                DateLost = GetForm(form, "dateLost"),
                TimeLost = GetForm(form, "timeLost"),
                ContactName = GetForm(form, "contactName"),
                ContactPhone = GetForm(form, "contactPhone"),
                CollectionPlace = GetForm(form, "collectionPlace"),
                Status = GetForm(form, "status"),
                AdminStatus = GetForm(form, "adminStatus"),
                StorageBinId = GetForm(form, "storageBinId"),
                Condition = GetForm(form, "condition"),
                AdminNotes = GetForm(form, "adminNotes"),
                ReportedColor = GetForm(form, "reportedColor"),
                IntakePhotoUrl = GetForm(form, "intakePhotoUrl"),
                ConfirmAccuracy = TryParseBool(GetForm(form, "confirmAccuracy")),
                ClaimantName = GetForm(form, "claimantName"),
                ClaimantStudentId = GetForm(form, "claimantStudentId"),
                StudentIdVerified = TryParseBool(GetForm(form, "studentIdVerified")),
                HandoverProofUrl = GetForm(form, "handoverProofUrl")
            };

            if (form.TryGetValue("photoUrls", out var photoUrls))
            {
                model.PhotoUrls = photoUrls
                    .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToList();
            }

            if (form.Files.Count > 0)
            {
                model.Files = form.Files.ToList();
            }

            return model;
        }

        try
        {
            var body = await JsonSerializer.DeserializeAsync<ReportRequest>(Request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return body;
        }
        catch
        {
            return null;
        }
    }

    private async Task<LostFoundItem> CreateItemAsync(ReportRequest request, LostFoundItemType type, bool isAdminCreate)
    {
        var imageUrls = new List<string>();
        if (request.PhotoUrls != null)
        {
            imageUrls.AddRange(request.PhotoUrls.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }

        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                var uploadedUrl = await SaveFileAsync(file);
                if (!string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    imageUrls.Add(uploadedUrl);
                }
            }
        }

        var postedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.DateLost) &&
            DateOnly.TryParse(request.DateLost, out var dateOnly))
        {
            postedAt = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        var item = new LostFoundItem
        {
            Type = type,
            Title = request.ItemName!.Trim(),
            Category = NormalizeCategory(request.Category!),
            Description = request.Description?.Trim(),
            LocationType = request.LocationType?.Trim(),
            BuildingCode = request.BuildingCode?.Trim(),
            IsRoom = request.IsRoom?.Trim(),
            RoomOrArea = request.RoomOrArea?.Trim(),
            CampusLocation = request.CampusLocation?.Trim(),
            Building = request.Building?.Trim(),
            Floor = request.Floor?.Trim(),
            RoomArea = request.RoomArea?.Trim(),
            LastKnownLocation = request.LastKnownLocation?.Trim(),
            TimeLost = request.TimeLost?.Trim(),
            ContactName = request.ContactName?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            CollectionPlace = request.CollectionPlace?.Trim(),
            PostedAt = postedAt,
            PostedByUserId = TryGetUserId(),
            Status = TryParsePublicStatus(request.Status, out var parsedStatus) ? parsedStatus : LostFoundPublicStatus.PendingVerification,
            AdminStatus = TryParseAdminStatus(request.AdminStatus, out var parsedAdminStatus) ? parsedAdminStatus : LostFoundAdminStatus.Pending,
            StorageBinId = request.StorageBinId,
            Condition = TryParseCondition(request.Condition, out var parsedCondition) ? parsedCondition : null,
            AdminNotes = request.AdminNotes,
            ReportedColor = request.ReportedColor,
            IntakePhotoUrl = request.IntakePhotoUrl,
            ConfirmAccuracy = request.ConfirmAccuracy,
            ClaimantName = request.ClaimantName,
            ClaimantStudentId = request.ClaimantStudentId,
            StudentIdVerified = request.StudentIdVerified,
            HandoverProofUrl = request.HandoverProofUrl,
            CreatedByUserId = TryGetUserId()
        };
        if (!isAdminCreate)
        {
            item.Status = LostFoundPublicStatus.PendingVerification;
            item.AdminStatus = LostFoundAdminStatus.Pending;
        }

        item.Location = BuildLocation(item);
        item.PrimaryImageUrl = imageUrls.FirstOrDefault();
        if (DateOnly.TryParse(request.DateLost, out var parsedDate))
        {
            item.DateLost = parsedDate;
        }

        _dbContext.LostFoundItems.Add(item);
        await _dbContext.SaveChangesAsync();

        item.ReferenceNumber = $"LF-{item.Id:D6}";
        item.Images = imageUrls.Select((url, i) => new LostFoundItemImage
        {
            LostFoundItemId = item.Id,
            Url = url,
            SortOrder = i
        }).ToList();

        _dbContext.LostFoundItemImages.AddRange(item.Images);
        await _dbContext.SaveChangesAsync();
        return item;
    }

    private object? ValidateReport(ReportRequest request, LostFoundItemType type, bool isAdminCreate = false)
    {
        var details = new Dictionary<string, List<string>>();
        void Add(string key, string message)
        {
            if (!details.ContainsKey(key)) details[key] = new List<string>();
            details[key].Add(message);
        }

        if (string.IsNullOrWhiteSpace(request.ItemName)) Add("itemName", "itemName is required.");
        if (string.IsNullOrWhiteSpace(request.Category)) Add("category", "category is required.");
        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Length > 500) Add("description", "description max length is 500.");
        if (!string.IsNullOrWhiteSpace(request.Status) && !TryParsePublicStatus(request.Status, out _))
            Add("status", "status must be Active or Pending Verification.");
        if (!string.IsNullOrWhiteSpace(request.AdminStatus) && !TryParseAdminStatus(request.AdminStatus, out _))
            Add("adminStatus", "adminStatus must be Pending, Received, or Delivered.");
        if (!string.IsNullOrWhiteSpace(request.Condition) && !TryParseCondition(request.Condition, out _))
            Add("condition", "condition must be good, fair, or damaged.");

        if (type == LostFoundItemType.Found)
        {
            if (string.IsNullOrWhiteSpace(request.LocationType)) Add("locationType", "locationType is required for found reports.");
            if (string.Equals(request.LocationType, "building", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.BuildingCode) && string.IsNullOrWhiteSpace(request.Building))
                {
                    Add("buildingCode", "buildingCode/building is required when locationType=building.");
                }

                if (string.IsNullOrWhiteSpace(request.RoomOrArea) && string.IsNullOrWhiteSpace(request.RoomArea))
                {
                    Add("roomOrArea", "roomOrArea/roomArea is required when locationType=building.");
                }
            }

            if (string.Equals(request.LocationType, "campus", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.CampusLocation))
            {
                Add("campusLocation", "campusLocation is required when locationType=campus.");
            }

            var totalPhotoCount = (request.PhotoUrls?.Count ?? 0) + (request.Files?.Count ?? 0);
            if (totalPhotoCount == 0) Add("photos", "At least one photo is required for found reports.");
            if (string.IsNullOrWhiteSpace(request.CollectionPlace)) Add("collectionPlace", "collectionPlace is required for found reports.");
        }

        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    Add("photos", "Only PNG/JPG files are supported.");
                }
            }
        }

        if (isAdminCreate && !string.IsNullOrWhiteSpace(request.Type))
        {
            if (!TryParseItemType(request.Type, out _))
            {
                Add("type", "type must be lost or found.");
            }
        }

        if (details.Count == 0) return null;
        return Error("VALIDATION_ERROR", "Validation failed.", details);
    }

    private object ToItemResponse(LostFoundItem item, bool includeAdminFields)
    {
        var daysAgo = Math.Max((int)(DateTime.UtcNow.Date - item.PostedAt.Date).TotalDays, 0);
        var imageUrls = item.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).ToList();
        var primary = item.PrimaryImageUrl ?? imageUrls.FirstOrDefault();

        return new
        {
            id = item.Id,
            type = ToItemTypeString(item.Type),
            title = item.Title,
            location = item.Location,
            category = item.Category,
            status = ToPublicStatusString(item.Status),
            adminStatus = includeAdminFields ? ToAdminStatusString(item.AdminStatus) : null,
            description = item.Description,
            referenceNumber = item.ReferenceNumber,
            datePosted = item.PostedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            daysAgo,
            postedAt = item.PostedAt,
            image = primary,
            images = imageUrls,
            postedByUserId = item.PostedByUserId,
            collectionPlace = item.CollectionPlace,
            locationType = item.LocationType,
            buildingCode = item.BuildingCode,
            isRoom = item.IsRoom,
            roomOrArea = item.RoomOrArea,
            campusLocation = item.CampusLocation,
            building = item.Building,
            floor = item.Floor,
            roomArea = item.RoomArea,
            lastKnownLocation = item.LastKnownLocation,
            dateLost = item.DateLost?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeLost = item.TimeLost,
            contactName = item.ContactName,
            contactPhone = item.ContactPhone,
            storageBinId = includeAdminFields ? item.StorageBinId : null,
            condition = includeAdminFields ? ToConditionString(item.Condition) : null,
            adminNotes = includeAdminFields ? item.AdminNotes : null,
            reportedColor = includeAdminFields ? item.ReportedColor : null,
            intakePhotoUrl = includeAdminFields ? item.IntakePhotoUrl : null,
            confirmAccuracy = includeAdminFields ? item.ConfirmAccuracy : null,
            claimantName = includeAdminFields ? item.ClaimantName : null,
            claimantStudentId = includeAdminFields ? item.ClaimantStudentId : null,
            studentIdVerified = includeAdminFields ? item.StudentIdVerified : null,
            handoverProofUrl = includeAdminFields ? item.HandoverProofUrl : null
        };
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

    private static string ToItemTypeString(LostFoundItemType type) => type == LostFoundItemType.Found ? "found" : "lost";

    private static string ToPublicStatusString(LostFoundPublicStatus status) =>
        status == LostFoundPublicStatus.Active ? "Active" : "Pending Verification";

    private static string? ToAdminStatusString(LostFoundAdminStatus? status) =>
        status?.ToString();

    private static string? ToConditionString(LostFoundItemCondition? condition)
    {
        return condition switch
        {
            LostFoundItemCondition.Good => "good",
            LostFoundItemCondition.Fair => "fair",
            LostFoundItemCondition.Damaged => "damaged",
            _ => null
        };
    }

    private static string ToClaimTypeString(LostFoundClaimType claimType) =>
        claimType == LostFoundClaimType.Owner ? "owner" : "finder";

    private static string ToClaimStatusString(LostFoundClaimStatus status)
    {
        return status switch
        {
            LostFoundClaimStatus.PendingReview => "pending_review",
            LostFoundClaimStatus.Approved => "approved",
            LostFoundClaimStatus.Rejected => "rejected",
            _ => "pending_review"
        };
    }

    private static object Error(string error, string message, Dictionary<string, List<string>>? details = null) =>
        new { error, message, details };

    private static string NormalizeCategory(string category)
    {
        if (string.Equals(category, "Clothing", StringComparison.OrdinalIgnoreCase)) return "Personal Items";
        if (string.Equals(category, "Other", StringComparison.OrdinalIgnoreCase)) return "Accessories";

        foreach (var canonical in CanonicalCategories)
        {
            if (string.Equals(category, canonical, StringComparison.OrdinalIgnoreCase))
            {
                return canonical;
            }
        }

        return category.Trim();
    }

    private string BuildLocation(LostFoundItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.BuildingCode)) parts.Add(item.BuildingCode);
        else if (!string.IsNullOrWhiteSpace(item.Building)) parts.Add(item.Building);
        if (!string.IsNullOrWhiteSpace(item.RoomOrArea)) parts.Add(item.RoomOrArea);
        else if (!string.IsNullOrWhiteSpace(item.RoomArea)) parts.Add(item.RoomArea);
        if (!string.IsNullOrWhiteSpace(item.CampusLocation)) parts.Add(item.CampusLocation);
        if (!string.IsNullOrWhiteSpace(item.LastKnownLocation)) parts.Add(item.LastKnownLocation);
        return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            return string.Empty;
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "lost-and-found");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"{Request.Scheme}://{Request.Host}/uploads/lost-and-found/{fileName}";
    }

    private string? TryGetUserId()
    {
        return User?.FindFirst("sub")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private bool IsAdmin()
    {
        return User?.Claims?.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            (string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(c.Value, "lost_found_leader", StringComparison.OrdinalIgnoreCase))) == true;
    }

    private static bool? TryParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return bool.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? GetForm(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out var value) ? value.ToString() : null;
    }

    public sealed class PatchItemRequest
    {
        public string? AdminStatus { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? StorageBinId { get; set; }
        public string? Condition { get; set; }
        public string? AdminNotes { get; set; }
        public string? ReportedColor { get; set; }
    }

    public sealed class NotifyRequest
    {
        public string? Channel { get; set; }
        public string? Message { get; set; }
    }

    public sealed class CreateClaimRequest
    {
        public string? ClaimType { get; set; }
        public string? Message { get; set; }
    }

    public sealed class ConfirmReceiptRequest
    {
        public string StorageBinId { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string? IntakePhotoUrl { get; set; }
        public bool ConfirmAccuracy { get; set; }
        public string? AdminNotes { get; set; }
    }

    public sealed class CompleteDeliveryRequest
    {
        public string? ClaimantName { get; set; }
        public string? ClaimantStudentId { get; set; }
        public bool StudentIdVerified { get; set; }
        public string? HandoverProofUrl { get; set; }
    }

    public sealed class ReportRequest
    {
        public string? Type { get; set; }
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? LocationType { get; set; }
        public string? BuildingCode { get; set; }
        public string? IsRoom { get; set; }
        public string? RoomOrArea { get; set; }
        public string? CampusLocation { get; set; }
        public string? Building { get; set; }
        public string? Floor { get; set; }
        public string? RoomArea { get; set; }
        public string? LastKnownLocation { get; set; }
        public string? DateLost { get; set; }
        public string? TimeLost { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public List<string>? PhotoUrls { get; set; }
        public List<IFormFile>? Files { get; set; }
        public string? CollectionPlace { get; set; }
        public string? Status { get; set; }
        public string? AdminStatus { get; set; }
        public string? StorageBinId { get; set; }
        public string? Condition { get; set; }
        public string? AdminNotes { get; set; }
        public string? ReportedColor { get; set; }
        public string? IntakePhotoUrl { get; set; }
        public bool? ConfirmAccuracy { get; set; }
        public string? ClaimantName { get; set; }
        public string? ClaimantStudentId { get; set; }
        public bool? StudentIdVerified { get; set; }
        public string? HandoverProofUrl { get; set; }
    }
}
