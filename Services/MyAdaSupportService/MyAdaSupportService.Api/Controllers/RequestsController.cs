using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.ServicesInterfaces;

namespace MyAdaSupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/Requests")]
[Authorize]
public class SupportRequestsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    };

    private readonly ISupportRequestService _supportRequestService;
    private readonly IAssignmentService _assignmentService;
    private readonly IRequestStatusService _requestStatusService;
    private readonly IFileStorageService _fileStorageService;

    public SupportRequestsController(
        ISupportRequestService supportRequestService,
        IAssignmentService assignmentService,
        IRequestStatusService requestStatusService,
        IFileStorageService fileStorageService)
    {
        _supportRequestService = supportRequestService;
        _assignmentService = assignmentService;
        _requestStatusService = requestStatusService;
        _fileStorageService = fileStorageService;
    }

    [HttpPost("uploads")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<string>>> UploadFiles([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0 || files.All(x => x.Length == 0))
            return BadRequest("No files were uploaded.");

        var uploadedPaths = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0)
                return BadRequest($"File '{file.FileName}' is empty.");

            if (file.Length > MaxFileSizeBytes)
                return BadRequest($"File '{file.FileName}' exceeds the 10 MB size limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !_fileStorageService.IsSupportedImage(file.FileName))
                return BadRequest($"Unsupported file type '{extension}'. Allowed types: jpg, jpeg, png, webp, gif.");

            await using var stream = file.OpenReadStream();
            var uploadedUrl = await _fileStorageService.UploadImageAsync(
                stream,
                file.FileName,
                file.ContentType,
                "requests/attachments",
                HttpContext.RequestAborted);

            uploadedPaths.Add(uploadedUrl);
        }

        return Ok(uploadedPaths);
    }

    [HttpPost("member/{memberId:guid}")]
    public async Task<ActionResult<SupportRequestDetailDto>> CreateRequest(
        Guid memberId,
        [FromBody] CreateSupportRequestDto dto)
    {
        var result = await _supportRequestService.CreateRequestAsync(dto, memberId);
        return Ok(result);
    }

    [HttpGet("member/{memberId:guid}")]
    public async Task<ActionResult<List<SupportRequestListItemDto>>> GetMyRequests(Guid memberId)
    {
        var result = await _supportRequestService.GetMyRequestsAsync(memberId);
        return Ok(result);
    }

    [HttpGet("staff/{staffId:guid}")]
    public async Task<ActionResult<List<SupportRequestListItemDto>>> GetAssignedRequests(Guid staffId)
    {
        var result = await _supportRequestService.GetAssignedRequestsAsync(staffId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<SupportRequestListItemDto>>> GetAllRequests([FromQuery] RequestFilterDto filter)
    {
        var result = await _supportRequestService.GetAllRequestsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{requestId:int}")]
    public async Task<ActionResult<SupportRequestDetailDto>> GetRequestDetail(int requestId)
    {
        var result = await _supportRequestService.GetRequestDetailAsync(requestId);
        return Ok(result);
    }

    [HttpGet("{requestId:int}/timeline")]
    public async Task<ActionResult<List<TimelineItemDto>>> GetRequestTimeline(int requestId)
    {
        var result = await _supportRequestService.GetRequestTimelineAsync(requestId);
        return Ok(result);
    }

    [HttpPut("{requestId:int}/cancel/member/{memberId:guid}")]
    public async Task<IActionResult> CancelRequest(
        int requestId,
        Guid memberId,
        [FromBody] CancelRequestDto dto)
    {
        await _supportRequestService.CancelRequestAsync(requestId, memberId, dto);
        return NoContent();
    }

    [HttpPut("{requestId:int}/assign/dispatcher/{dispatcherId:guid}")]
    public async Task<IActionResult> AssignRequest(
        int requestId,
        Guid dispatcherId,
        [FromBody] AssignRequestDto dto)
    {
        await _assignmentService.AssignRequestAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/reassign/dispatcher/{dispatcherId:guid}")]
    public async Task<IActionResult> ReassignRequest(
        int requestId,
        Guid dispatcherId,
        [FromBody] AssignRequestDto dto)
    {
        await _assignmentService.ReassignRequestAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/dispatcher-instructions/dispatcher/{dispatcherId:guid}")]
    public async Task<IActionResult> AddDispatcherInstructions(
        int requestId,
        Guid dispatcherId,
        [FromBody] DispatcherInstructionDto dto)
    {
        await _assignmentService.AddDispatcherInstructionsAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/start/staff/{staffId:guid}")]
    public async Task<IActionResult> MarkAsStarted(int requestId, Guid staffId)
    {
        await _requestStatusService.MarkAsStartedAsync(requestId, staffId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/in-progress/staff/{staffId:guid}")]
    public async Task<IActionResult> MarkAsInProgress(int requestId, Guid staffId)
    {
        await _requestStatusService.MarkAsInProgressAsync(requestId, staffId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/complete/staff/{staffId:guid}")]
    public async Task<IActionResult> MarkAsCompleted(int requestId, Guid staffId)
    {
        await _requestStatusService.MarkAsCompletedAsync(requestId, staffId);
        return NoContent();
    }
}
