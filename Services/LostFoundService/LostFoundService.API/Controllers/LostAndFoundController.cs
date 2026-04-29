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
    private static readonly HashSet<string> UserLostReportJsonFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "status"
    };

    private static readonly HashSet<string> UserLostReportFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "status",
        "files",
        "files[]"
    };

    private static readonly HashSet<string> UserLostReportFileFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "files",
        "files[]"
    };

    private static readonly HashSet<string> FoundReportJsonFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "locationType",
        "buildingCode",
        "isRoom",
        "roomOrArea",
        "campusLocation",
        "building",
        "floor",
        "roomArea",
        "lastKnownLocation",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "collectionPlace",
        "status",
        "adminStatus",
        "storageBinId",
        "condition",
        "adminNotes",
        "reportedColor",
        "confirmAccuracy",
        "claimantName",
        "claimantStudentId",
        "studentIdVerified"
    };

    private static readonly HashSet<string> FoundReportFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "customLocation",
        "locationType",
        "buildingCode",
        "isRoom",
        "roomOrArea",
        "campusLocation",
        "building",
        "floor",
        "roomArea",
        "lastKnownLocation",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "collectionPlace",
        "status",
        "adminStatus",
        "storageBinId",
        "condition",
        "adminNotes",
        "reportedColor",
        "confirmAccuracy",
        "claimantName",
        "claimantStudentId",
        "studentIdVerified",
        "files",
        "files[]"
    };

    private static readonly HashSet<string> FoundReportFileFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "files",
        "files[]",
        "intakePhoto",
        "intakePhotoFile",
        "handoverProof",
        "handoverProofFile"
    };

    private static readonly HashSet<string> ManualCreateJsonFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "locationType",
        "buildingCode",
        "isRoom",
        "roomOrArea",
        "campusLocation",
        "building",
        "floor",
        "roomArea",
        "lastKnownLocation",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "collectionPlace",
        "status",
        "adminStatus",
        "storageBinId",
        "condition",
        "adminNotes",
        "reportedColor"
    };

    private static readonly HashSet<string> ManualCreateFormFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "type",
        "itemName",
        "categoryId",
        "description",
        "location",
        "customLocation",
        "locationType",
        "buildingCode",
        "isRoom",
        "roomOrArea",
        "campusLocation",
        "building",
        "floor",
        "roomArea",
        "lastKnownLocation",
        "dateLost",
        "timeLost",
        "contactName",
        "contactPhone",
        "collectionPlace",
        "status",
        "adminStatus",
        "storageBinId",
        "condition",
        "adminNotes",
        "reportedColor",
        "files",
        "files[]"
    };

    private static readonly HashSet<string> ManualCreateFileFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "files",
        "files[]"
    };

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
        [FromQuery] int? categoryId,
        [FromQuery] string? status,
        [FromQuery] string? adminStatus,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var result = await _lostAndFoundService.GetItemsAsync(new LostFoundItemSearchQuery
            {
                Q = q,
                Search = search,
                Type = type,
                PostedBy = postedBy,
                CategoryId = categoryId,
                Status = status,
                AdminStatus = adminStatus,
                Page = page,
                Limit = limit,
                CurrentUserId = TryGetUserId(),
                IncludeAdminFields = false,
                ExcludeTerminalAdminItems = !IsAdmin()
            });

            return Ok(new ItemListResponse
            {
                Items = result.Items.Select(item => MapItemResponse(item, includeAdminFields: false)).ToList(),
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
            var item = await _lostAndFoundService.GetItemByIdAsync(
                id,
                currentUserId: TryGetUserId(),
                hideTerminalItemsForPublic: !IsAdmin());
            return Ok(MapItemResponse(item, includeAdminFields: false));
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
        var (model, files, readError) = await ReadUserLostReportRequestAsync();
        if (readError != null)
        {
            return BadRequest(readError);
        }

        if (model == null)
        {
            return BadRequest(Error("VALIDATION_ERROR", "Invalid request payload."));
        }

        var validationError = ValidateUserLostReportRequest(model);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var command = _mapper.Map<CreateLostFoundItemCommand>(model);
        command.Type = "lost";
        command.Status = string.IsNullOrWhiteSpace(model.Status) ? "pending" : model.Status.Trim();
        command.IsAdminCreate = false;
        command.PostedByUserId = TryGetUserId();
        command.CreatedByUserId = TryGetUserId();

        var fileError = await PopulateCreateCommandFilesAsync(command, files);
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
        var (model, readError) = await ReadFoundReportRequestAsync();
        if (readError != null)
        {
            return BadRequest(readError);
        }

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
        var (model, files, readError) = await ReadManualCreateRequestAsync();
        if (readError != null)
        {
            return BadRequest(readError);
        }

        if (model == null || string.IsNullOrWhiteSpace(model.Type))
        {
            return BadRequest(Error("VALIDATION_ERROR", "type is required for manual create."));
        }

        var command = _mapper.Map<CreateLostFoundItemCommand>(model);
        command.IsAdminCreate = true;
        command.PostedByUserId = TryGetUserId();
        command.CreatedByUserId = TryGetUserId();

        var fileError = await PopulateCreateCommandFilesAsync(command, files);
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
        IFormFile? file,
        IFormFile? photo,
        IFormFile? image,
        IFormFile? imageFile)
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
                Status = response.Status,
                IsMine = string.Equals(response.ClaimType, "owner", StringComparison.OrdinalIgnoreCase),
                ItemFound = string.Equals(response.ClaimType, "finder", StringComparison.OrdinalIgnoreCase)
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
            var item = await _lostAndFoundService.GetTimelineItemAsync(id, hideTerminalItemsForPublic: !IsAdmin());
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

    private async Task<(ManualCreateItemRequest? Model, List<IFormFile> Files, ErrorResponse? Error)> ReadManualCreateRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var unsupportedFields = form.Keys
                .Where(key => !ManualCreateFormFields.Contains(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var unsupportedFileFields = form.Files
                .Select(file => file.Name)
                .Where(name => !ManualCreateFileFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            unsupportedFields.AddRange(
                unsupportedFileFields.Where(name =>
                    !unsupportedFields.Contains(name, StringComparer.OrdinalIgnoreCase)));

            if (unsupportedFields.Count > 0)
            {
                return (null, new List<IFormFile>(), UnsupportedManualCreateFieldsError(unsupportedFields));
            }

            return (new ManualCreateItemRequest
            {
                Type = GetForm(form, "type"),
                ItemName = GetForm(form, "itemName"),
                CategoryId = GetFormInt(form, "categoryId"),
                Description = GetForm(form, "description"),
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
                ReportedColor = GetForm(form, "reportedColor")
            }, form.Files.ToList(), null);
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: HttpContext.RequestAborted);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, new List<IFormFile>(), Error("VALIDATION_ERROR", "Invalid request payload."));
            }

            var unsupportedFields = document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !ManualCreateJsonFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unsupportedFields.Count > 0)
            {
                return (null, new List<IFormFile>(), UnsupportedManualCreateFieldsError(unsupportedFields));
            }

            var model = document.RootElement.Deserialize<ManualCreateItemRequest>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (model, new List<IFormFile>(), null);
        }
        catch
        {
            return (null, new List<IFormFile>(), Error("VALIDATION_ERROR", "Invalid request payload."));
        }
    }

    private async Task<(UserLostReportRequest? Model, List<IFormFile> Files, ErrorResponse? Error)> ReadUserLostReportRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var unsupportedFields = form.Keys
                .Where(key => !UserLostReportFormFields.Contains(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var unsupportedFileFields = form.Files
                .Select(file => file.Name)
                .Where(name => !UserLostReportFileFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            unsupportedFields.AddRange(
                unsupportedFileFields.Where(name =>
                    !unsupportedFields.Contains(name, StringComparer.OrdinalIgnoreCase)));

            if (unsupportedFields.Count > 0)
            {
                return (null, new List<IFormFile>(), UnsupportedUserLostReportFieldsError(unsupportedFields));
            }

            return (new UserLostReportRequest
            {
                Type = GetForm(form, "type"),
                ItemName = GetForm(form, "itemName"),
                CategoryId = GetFormInt(form, "categoryId"),
                Description = GetForm(form, "description"),
                Location = GetForm(form, "location"),
                DateLost = GetForm(form, "dateLost"),
                TimeLost = GetForm(form, "timeLost"),
                ContactName = GetForm(form, "contactName"),
                ContactPhone = GetForm(form, "contactPhone"),
                Status = GetForm(form, "status")
            }, form.Files.ToList(), null);
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: HttpContext.RequestAborted);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, new List<IFormFile>(), Error("VALIDATION_ERROR", "Invalid request payload."));
            }

            var unsupportedFields = document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !UserLostReportJsonFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unsupportedFields.Count > 0)
            {
                return (null, new List<IFormFile>(), UnsupportedUserLostReportFieldsError(unsupportedFields));
            }

            var model = document.RootElement.Deserialize<UserLostReportRequest>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (model, new List<IFormFile>(), null);
        }
        catch
        {
            return (null, new List<IFormFile>(), Error("VALIDATION_ERROR", "Invalid request payload."));
        }
    }

    private async Task<(ReportRequest? Model, ErrorResponse? Error)> ReadFoundReportRequestAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var unsupportedFields = form.Keys
                .Where(key => !FoundReportFormFields.Contains(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var unsupportedFileFields = form.Files
                .Select(file => file.Name)
                .Where(name => !FoundReportFileFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            unsupportedFields.AddRange(
                unsupportedFileFields.Where(name =>
                    !unsupportedFields.Contains(name, StringComparer.OrdinalIgnoreCase)));

            if (unsupportedFields.Count > 0)
            {
                return (null, UnsupportedFoundReportFieldsError(unsupportedFields));
            }

            var model = new ReportRequest
            {
                Type = GetForm(form, "type"),
                ItemName = GetForm(form, "itemName"),
                CategoryId = GetFormInt(form, "categoryId"),
                Description = GetForm(form, "description"),
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

            return (model, null);
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: HttpContext.RequestAborted);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (null, Error("VALIDATION_ERROR", "Invalid request payload."));
            }

            var unsupportedFields = document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .Where(name => !FoundReportJsonFields.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unsupportedFields.Count > 0)
            {
                return (null, UnsupportedFoundReportFieldsError(unsupportedFields));
            }

            var model = document.RootElement.Deserialize<ReportRequest>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (model, null);
        }
        catch
        {
            return (null, Error("VALIDATION_ERROR", "Invalid request payload."));
        }
    }

    private static ErrorResponse? ValidateUserLostReportRequest(UserLostReportRequest request)
    {
        var details = new Dictionary<string, List<string>>();

        if (!string.IsNullOrWhiteSpace(request.Type) &&
            !string.Equals(request.Type.Trim(), "lost", StringComparison.OrdinalIgnoreCase))
        {
            details["type"] = new List<string> { "type must be lost for this endpoint." };
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status.Trim(), "pending", StringComparison.OrdinalIgnoreCase))
        {
            details["status"] = new List<string> { "status must be pending for this endpoint." };
        }

        if (details.Count == 0)
        {
            return null;
        }

        return Error("VALIDATION_ERROR", "Validation failed.", details);
    }

    private async Task<ErrorResponse?> PopulateCreateCommandFilesAsync(
        CreateLostFoundItemCommand command,
        ReportRequest request)
    {
        var imageUploadError = await PopulateCreateCommandFilesAsync(command, request.Files);
        if (imageUploadError != null)
        {
            return imageUploadError;
        }

        if (request.IntakePhotoFile != null)
        {
            if (!_imageStorageService.IsSupportedImage(request.IntakePhotoFile.FileName))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            var intakePhotoUrl = await SaveFileAsync(request.IntakePhotoFile, "intake");
            if (string.IsNullOrWhiteSpace(intakePhotoUrl))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            command.IntakePhotoUrl = intakePhotoUrl;
        }

        if (request.HandoverProofFile != null)
        {
            if (!_imageStorageService.IsSupportedImage(request.HandoverProofFile.FileName))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            var handoverProofUrl = await SaveFileAsync(request.HandoverProofFile, "handover");
            if (string.IsNullOrWhiteSpace(handoverProofUrl))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            command.HandoverProofUrl = handoverProofUrl;
        }

        return null;
    }

    private async Task<ErrorResponse?> PopulateCreateCommandFilesAsync(
        CreateLostFoundItemCommand command,
        IEnumerable<IFormFile>? files)
    {
        var uploads = files?.ToList() ?? new List<IFormFile>();
        if (uploads.Any(file => !_imageStorageService.IsSupportedImage(file.FileName)))
        {
            return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
        }

        var imageUrls = new List<string>();
        foreach (var file in uploads)
        {
            var uploadedUrl = await SaveFileAsync(file);
            if (string.IsNullOrWhiteSpace(uploadedUrl))
            {
                return Error("VALIDATION_ERROR", "Only PNG/JPG files are supported.");
            }

            imageUrls.Add(uploadedUrl);
        }

        command.ImageUrls = NormalizeDistinctImageUrls(imageUrls);
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

    private static ErrorResponse UnsupportedUserLostReportFieldsError(IEnumerable<string> fields)
    {
        var values = fields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .Select(field => $"'{field}' is not supported on the user lost report endpoint.")
            .ToList();

        return Error(
            "VALIDATION_ERROR",
            "Only the user lost report fields are allowed on this endpoint.",
            new Dictionary<string, List<string>>
            {
                ["unsupportedFields"] = values
            });
    }

    private static ErrorResponse UnsupportedManualCreateFieldsError(IEnumerable<string> fields)
    {
        var values = fields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .Select(field => $"'{field}' is not supported on the manual create endpoint.")
            .ToList();

        return Error(
            "VALIDATION_ERROR",
            "Only manual create fields are allowed on this endpoint.",
            new Dictionary<string, List<string>>
            {
                ["unsupportedFields"] = values
            });
    }

    private static ErrorResponse UnsupportedFoundReportFieldsError(IEnumerable<string> fields)
    {
        var values = fields
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .Select(field => $"'{field}' is not supported on the found report endpoint. Attach image files using multipart/form-data file fields.")
            .ToList();

        return Error(
            "VALIDATION_ERROR",
            "Found reports accept image attachments as files, not body URL fields.",
            new Dictionary<string, List<string>>
            {
                ["unsupportedFields"] = values
            });
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

    private static int? GetFormInt(IFormCollection form, string key)
    {
        var value = GetForm(form, key);
        return int.TryParse(value, out var parsed) ? parsed : null;
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
