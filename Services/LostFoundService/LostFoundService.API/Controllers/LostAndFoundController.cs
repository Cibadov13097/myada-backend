using AutoMapper;
using System.Security.Claims;
using System.Text.Json;
using LostFoundService.Application.DTOs.LostAndFound;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Api.Models.LostAndFound.Requests;
using LostFoundService.Api.Models.LostAndFound.Responses;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFoundService.Api.Controllers;

[ApiController]
[Route("api/lost-and-found")]
public class LostAndFoundController : ControllerBase
{
    private readonly ILostAndFoundService _lostAndFoundService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IMapper _mapper;

    public LostAndFoundController(
        ILostAndFoundService lostAndFoundService,
        IImageStorageService imageStorageService,
        IMapper mapper)
    {
        _lostAndFoundService = lostAndFoundService;
        _imageStorageService = imageStorageService;
        _mapper = mapper;
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(ItemListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ItemListResponse>> GetItems(
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
        try
        {
            var includeAdminFields = IsAdmin();
            var result = await _lostAndFoundService.GetItemsAsync(new LostFoundItemSearchQuery
            {
                Q = q,
                Search = search,
                Type = type,
                PostedBy = postedBy,
                Category = category,
                Status = status,
                AdminStatus = adminStatus,
                Page = page,
                Limit = limit,
                CurrentUserId = TryGetUserId(),
                IncludeAdminFields = includeAdminFields
            });

            return Ok(new ItemListResponse
            {
                Items = result.Items.Select(item => MapItemResponse(item, includeAdminFields)).ToList(),
                Total = result.Total
            });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(Error("UNAUTHORIZED", ex.Message));
        }
    }

    [HttpGet("items/{id:int}")]
    [ProducesResponseType(typeof(LostFoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LostFoundItemResponse>> GetItemById(int id)
    {
        try
        {
            var item = await _lostAndFoundService.GetItemByIdAsync(id);
            return Ok(MapItemResponse(item, IsAdmin()));
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(StatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsResponse>> GetStats()
    {
        var stats = await _lostAndFoundService.GetStatsAsync();
        return Ok(_mapper.Map<StatsResponse>(stats));
    }

    [HttpPost("reports/lost")]
    [Consumes("application/json", "multipart/form-data")]
    [ProducesResponseType(typeof(ReportCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportCreatedResponse>> CreateLostReport()
    {
        var model = await ReadReportRequestAsync();
        if (model == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "Invalid request payload."));
        }

        var command = _mapper.Map<CreateLostFoundItemCommand>(model);
        command.Type = "lost";
        command.IsAdminCreate = false;
        command.PostedByUserId = TryGetUserId();
        command.CreatedByUserId = TryGetUserId();

        var fileError = await PopulateCreateCommandFilesAsync(command, model);
        if (fileError != null)
        {
            return BadRequest(fileError);
        }

        try
        {
            var item = await _lostAndFoundService.CreateItemAsync(command);
            var response = MapItemResponse(item, includeAdminFields: false);

            return StatusCode(StatusCodes.Status201Created, new ReportCreatedResponse
            {
                Id = item.Id,
                ReferenceNumber = item.ReferenceNumber,
                Status = response.Status,
                Message = "Lost item report submitted for review."
            });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
    }

    [HttpPost("reports/found")]
    [Consumes("application/json", "multipart/form-data")]
    [ProducesResponseType(typeof(ReportCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportCreatedResponse>> CreateFoundReport()
    {
        var model = await ReadReportRequestAsync();
        if (model == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "Invalid request payload."));
        }

        var command = _mapper.Map<CreateLostFoundItemCommand>(model);
        command.Type = "found";
        command.IsAdminCreate = false;
        command.PostedByUserId = TryGetUserId();
        command.CreatedByUserId = TryGetUserId();

        var fileError = await PopulateCreateCommandFilesAsync(command, model);
        if (fileError != null)
        {
            return BadRequest(fileError);
        }

        try
        {
            var item = await _lostAndFoundService.CreateItemAsync(command);
            var response = MapItemResponse(item, includeAdminFields: false);

            return StatusCode(StatusCodes.Status201Created, new ReportCreatedResponse
            {
                Id = item.Id,
                ReferenceNumber = item.ReferenceNumber,
                Status = response.Status,
                Message = "Found item report submitted for review."
            });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPatch("items/{id:int}")]
    [ProducesResponseType(typeof(LostFoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LostFoundItemResponse>> UpdateItem(int id, [FromBody] PatchItemRequest request)
    {
        var command = _mapper.Map<UpdateLostFoundItemCommand>(request);
        command.ModifiedByUserId = TryGetUserId();

        try
        {
            var item = await _lostAndFoundService.UpdateItemAsync(id, command);
            return Ok(MapItemResponse(item, includeAdminFields: true));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items")]
    [Consumes("application/json", "multipart/form-data")]
    [ProducesResponseType(typeof(LostFoundItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LostFoundItemResponse>> CreateManualItem()
    {
        var model = await ReadReportRequestAsync();
        if (model == null || string.IsNullOrWhiteSpace(model.Type))
        {
            return BadRequest(Error("VALIDATION_ERROR", "type is required for manual create."));
        }

        var command = _mapper.Map<CreateLostFoundItemCommand>(model);
        command.IsAdminCreate = true;
        command.PostedByUserId = TryGetUserId();
        command.CreatedByUserId = TryGetUserId();

        var fileError = await PopulateCreateCommandFilesAsync(command, model);
        if (fileError != null)
        {
            return BadRequest(fileError);
        }

        try
        {
            var item = await _lostAndFoundService.CreateItemAsync(command);
            return StatusCode(StatusCodes.Status201Created, MapItemResponse(item, includeAdminFields: true));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> Upload(
        [FromForm] IFormFile? file,
        [FromForm] IFormFile? photo,
        [FromForm] IFormFile? image,
        [FromForm] IFormFile? imageFile)
    {
        var upload = file ?? photo ?? image ?? imageFile;
        if (upload == null && Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            upload = form.Files.GetFile("file")
                ?? form.Files.GetFile("photo")
                ?? form.Files.GetFile("image")
                ?? form.Files.GetFile("imageFile")
                ?? form.Files.FirstOrDefault();
        }

        if (upload == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "file, photo, image or imageFile is required."));
        }

        var url = await SaveFileAsync(upload);
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(Error("VALIDATION_ERROR", "Only PNG/JPG files are supported."));
        }

        return StatusCode(StatusCodes.Status201Created, new UploadResponse
        {
            Url = url
        });
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/notify")]
    [ProducesResponseType(typeof(NotifyResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotifyResponse>> NotifyOwner(int id, [FromBody] NotifyRequest request)
    {
        var command = _mapper.Map<NotifyLostFoundOwnerCommand>(request);
        command.ModifiedByUserId = TryGetUserId();

        try
        {
            await _lostAndFoundService.NotifyOwnerAsync(id, command);
            return StatusCode(StatusCodes.Status202Accepted, new NotifyResponse
            {
                Queued = true
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [Authorize]
    [HttpPost("items/{id:int}/claims")]
    [ProducesResponseType(typeof(ClaimCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClaimCreatedResponse>> CreateClaim(int id, [FromBody] CreateClaimRequest request)
    {
        var command = _mapper.Map<CreateLostFoundClaimCommand>(request);
        var userId = TryGetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(Error("UNAUTHORIZED", "User is not authenticated."));
        }

        command.UserId = userId;

        try
        {
            var claim = await _lostAndFoundService.CreateClaimAsync(id, command);
            var response = _mapper.Map<ItemClaimResponse>(claim);

            return StatusCode(StatusCodes.Status201Created, new ClaimCreatedResponse
            {
                ClaimId = claim.Id,
                Status = response.Status
            });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
        catch (ConflictException ex)
        {
            return Conflict(ToError(ex));
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/confirm-receipt")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LostFoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LostFoundItemResponse>> ConfirmReceipt(int id, [FromForm] ConfirmReceiptRequest request)
    {
        var command = _mapper.Map<ConfirmLostFoundReceiptCommand>(request);
        command.ModifiedByUserId = TryGetUserId();

        if (request.IntakePhotoFile != null)
        {
            var intakePhotoUrl = await SaveFileAsync(request.IntakePhotoFile, "intake");
            if (string.IsNullOrWhiteSpace(intakePhotoUrl))
            {
                return BadRequest(Error("VALIDATION_ERROR", "Only PNG/JPG files are supported."));
            }

            command.IntakePhotoUrl = intakePhotoUrl;
        }

        try
        {
            var item = await _lostAndFoundService.ConfirmReceiptAsync(id, command);
            return Ok(MapItemResponse(item, includeAdminFields: true));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpPost("items/{id:int}/complete-delivery")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LostFoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LostFoundItemResponse>> CompleteDelivery(int id, [FromForm] CompleteDeliveryRequest request)
    {
        var command = _mapper.Map<CompleteLostFoundDeliveryCommand>(request);
        command.ModifiedByUserId = TryGetUserId();

        if (request.HandoverProofFile != null)
        {
            var handoverProofUrl = await SaveFileAsync(request.HandoverProofFile, "handover");
            if (string.IsNullOrWhiteSpace(handoverProofUrl))
            {
                return BadRequest(Error("VALIDATION_ERROR", "Only PNG/JPG files are supported."));
            }

            command.HandoverProofUrl = handoverProofUrl;
        }

        try
        {
            var item = await _lostAndFoundService.CompleteDeliveryAsync(id, command);
            return Ok(MapItemResponse(item, includeAdminFields: true));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(ToError(ex));
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [Authorize(Roles = "admin,lost_found_leader")]
    [HttpGet("items/{id:int}/claims")]
    [ProducesResponseType(typeof(ItemClaimsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemClaimsResponse>> GetItemClaims(int id)
    {
        try
        {
            var claims = await _lostAndFoundService.GetItemClaimsAsync(id);
            return Ok(new ItemClaimsResponse
            {
                Claims = _mapper.Map<List<ItemClaimResponse>>(claims)
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
    }

    [HttpGet("items/{id:int}/timeline")]
    [ProducesResponseType(typeof(TimelineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimelineResponse>> GetTimeline(int id)
    {
        try
        {
            var item = await _lostAndFoundService.GetTimelineItemAsync(id);
            return Ok(new TimelineResponse
            {
                Events = new List<TimelineEventResponse>
                {
                    new() { At = item.PostedAt, Label = "Report submitted", Done = true },
                    new() { At = item.LastNotifiedAt, Label = "Owner notified", Done = item.LastNotifiedAt.HasValue },
                    new() { At = item.DeliveryCompletedAt, Label = "Delivery completed", Done = item.DeliveryCompletedAt.HasValue }
                }
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(Error("NOT_FOUND", ex.Message));
        }
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
                ImageUrl = GetForm(form, "imageUrl"),
                Location = GetForm(form, "location") ?? GetForm(form, "customLocation"),
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
                ConfirmAccuracy = TryParseBool(GetForm(form, "confirmAccuracy")),
                ClaimantName = GetForm(form, "claimantName"),
                ClaimantStudentId = GetForm(form, "claimantStudentId"),
                StudentIdVerified = TryParseBool(GetForm(form, "studentIdVerified"))
            };

            if (form.Files.Count > 0)
            {
                var reportFiles = new List<IFormFile>();
                foreach (var file in form.Files)
                {
                    if (string.Equals(file.Name, "intakePhoto", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(file.Name, "intakePhotoFile", StringComparison.OrdinalIgnoreCase))
                    {
                        model.IntakePhotoFile = file;
                        continue;
                    }

                    if (string.Equals(file.Name, "handoverProof", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(file.Name, "handoverProofFile", StringComparison.OrdinalIgnoreCase))
                    {
                        model.HandoverProofFile = file;
                        continue;
                    }

                    reportFiles.Add(file);
                }

                if (reportFiles.Count > 0)
                {
                    model.Files = reportFiles;
                }
            }

            var imageUrls = ExtractImageUrlsFromForm(form);
            if (imageUrls.Count > 0)
            {
                model.ImageUrls = imageUrls;
            }

            return model;
        }

        try
        {
            return await JsonSerializer.DeserializeAsync<ReportRequest>(Request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private async Task<ErrorResponse?> PopulateCreateCommandFilesAsync(
        CreateLostFoundItemCommand command,
        ReportRequest request)
    {
        var imageUrls = CollectImageUrls(request);

        if (request.Files != null && request.Files.Any(file => !_imageStorageService.IsSupportedImage(file.FileName)))
        {
            return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
        }

        if (request.IntakePhotoFile != null && !_imageStorageService.IsSupportedImage(request.IntakePhotoFile.FileName))
        {
            return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
        }

        if (request.HandoverProofFile != null && !_imageStorageService.IsSupportedImage(request.HandoverProofFile.FileName))
        {
            return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
        }

        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                var uploadedUrl = await SaveFileAsync(file);
                if (string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
                }

                imageUrls.Add(uploadedUrl);
            }
        }

        command.ImageUrls = NormalizeDistinctImageUrls(imageUrls);

        if (request.IntakePhotoFile != null)
        {
            var intakePhotoUrl = await SaveFileAsync(request.IntakePhotoFile, "intake");
            if (string.IsNullOrWhiteSpace(intakePhotoUrl))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            command.IntakePhotoUrl = intakePhotoUrl;
        }

        if (request.HandoverProofFile != null)
        {
            var handoverProofUrl = await SaveFileAsync(request.HandoverProofFile, "handover");
            if (string.IsNullOrWhiteSpace(handoverProofUrl))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            command.HandoverProofUrl = handoverProofUrl;
        }

        return null;
    }

    private LostFoundItemResponse MapItemResponse(LostFoundItem item, bool includeAdminFields)
    {
        return _mapper.Map<LostFoundItemResponse>(item, options =>
        {
            options.Items["IncludeAdminFields"] = includeAdminFields;
        });
    }

    private async Task<string?> SaveFileAsync(IFormFile file, string directory = "items")
    {
        if (!_imageStorageService.IsSupportedImage(file.FileName))
        {
            return string.Empty;
        }

        await using var stream = file.OpenReadStream();
        return await _imageStorageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            directory,
            HttpContext.RequestAborted);
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

    private static ErrorResponse ToError(BadRequestException exception)
    {
        return Error(exception.ErrorCode, exception.Message, exception.Details);
    }

    private static ErrorResponse ToError(ConflictException exception)
    {
        return Error(exception.ErrorCode, exception.Message);
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

    private static List<string> ExtractImageUrlsFromForm(IFormCollection form)
    {
        var urls = new List<string>();
        var keys = new[]
        {
            "imageUrl",
            "imageUrls",
            "imageUrls[]",
            "images",
            "images[]",
            "photoUrls",
            "photoUrls[]",
            "photos",
            "photos[]"
        };

        foreach (var key in keys)
        {
            if (!form.TryGetValue(key, out var values))
            {
                continue;
            }

            foreach (var value in values)
            {
                AppendImageUrlTokens(urls, value);
            }
        }

        return NormalizeDistinctImageUrls(urls);
    }

    private static List<string> CollectImageUrls(ReportRequest request)
    {
        var imageUrls = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            AppendImageUrlTokens(imageUrls, request.ImageUrl);
        }

        if (request.ImageUrls != null)
        {
            foreach (var imageUrl in request.ImageUrls)
            {
                AppendImageUrlTokens(imageUrls, imageUrl);
            }
        }

        return NormalizeDistinctImageUrls(imageUrls);
    }

    private static void AppendImageUrlTokens(List<string> target, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        var trimmed = rawValue.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    foreach (var value in parsed)
                    {
                        AppendImageUrlTokens(target, value);
                    }

                    return;
                }
            }
            catch
            {
                // Fall back to storing the raw value below.
            }
        }

        target.Add(trimmed);
    }

    private static List<string> NormalizeDistinctImageUrls(IEnumerable<string> imageUrls)
    {
        return imageUrls
            .Select(url => url.Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ErrorResponse Error(string error, string message, Dictionary<string, List<string>>? details = null) =>
        new()
        {
            Error = error,
            Message = message,
            Details = details
        };
}
