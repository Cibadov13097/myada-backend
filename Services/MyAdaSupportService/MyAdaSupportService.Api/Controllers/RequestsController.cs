using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.ServicesInterfaces;
using System.Text.RegularExpressions;

namespace MyAdaSupportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    private readonly IWebHostEnvironment _environment;

    public SupportRequestsController(
        ISupportRequestService supportRequestService,
        IAssignmentService assignmentService,
        IRequestStatusService requestStatusService,
        IWebHostEnvironment environment)
    {
        _supportRequestService = supportRequestService;
        _assignmentService = assignmentService;
        _requestStatusService = requestStatusService;
        _environment = environment;
    }

    [HttpPost("uploads")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<string>>> UploadFiles([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0 || files.All(x => x.Length == 0))
            return BadRequest("No files were uploaded.");

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadDirectory = Path.Combine(webRoot, "support-ticket-uploads");
        Directory.CreateDirectory(uploadDirectory);

        var uploadedPaths = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0)
                return BadRequest($"File '{file.FileName}' is empty.");

            if (file.Length > MaxFileSizeBytes)
                return BadRequest($"File '{file.FileName}' exceeds the 10 MB size limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest($"Unsupported file type '{extension}'. Allowed types: jpg, jpeg, png, webp, gif.");

            var safeBaseName = SanitizeFileBaseName(Path.GetFileNameWithoutExtension(file.FileName));
            var generatedName = GenerateUniqueFileName(uploadDirectory, safeBaseName, extension);
            var fullPath = Path.Combine(uploadDirectory, generatedName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            uploadedPaths.Add($"/support-ticket-uploads/{generatedName}");
        }

        return Ok(uploadedPaths);
    }

    [HttpPost("member/{memberId:int}")]
    public async Task<ActionResult<SupportRequestDetailDto>> CreateRequest(
        int memberId,
        [FromBody] CreateSupportRequestDto dto)
    {
        var result = await _supportRequestService.CreateRequestAsync(dto, memberId);
        return Ok(result);
    }

    [HttpGet("member/{memberId:int}")]
    public async Task<ActionResult<List<SupportRequestListItemDto>>> GetMyRequests(int memberId)
    {
        var result = await _supportRequestService.GetMyRequestsAsync(memberId);
        return Ok(result);
    }

    [HttpGet("staff/{staffId:int}")]
    public async Task<ActionResult<List<SupportRequestListItemDto>>> GetAssignedRequests(int staffId)
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

    [HttpPut("{requestId:int}/cancel/member/{memberId:int}")]
    public async Task<IActionResult> CancelRequest(
        int requestId,
        int memberId,
        [FromBody] CancelRequestDto dto)
    {
        await _supportRequestService.CancelRequestAsync(requestId, memberId, dto);
        return NoContent();
    }

    [HttpPut("{requestId:int}/assign/dispatcher/{dispatcherId:int}")]
    public async Task<IActionResult> AssignRequest(
        int requestId,
        int dispatcherId,
        [FromBody] AssignRequestDto dto)
    {
        await _assignmentService.AssignRequestAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/reassign/dispatcher/{dispatcherId:int}")]
    public async Task<IActionResult> ReassignRequest(
        int requestId,
        int dispatcherId,
        [FromBody] AssignRequestDto dto)
    {
        await _assignmentService.ReassignRequestAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/dispatcher-instructions/dispatcher/{dispatcherId:int}")]
    public async Task<IActionResult> AddDispatcherInstructions(
        int requestId,
        int dispatcherId,
        [FromBody] DispatcherInstructionDto dto)
    {
        await _assignmentService.AddDispatcherInstructionsAsync(requestId, dto, dispatcherId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/start/staff/{staffId:int}")]
    public async Task<IActionResult> MarkAsStarted(int requestId, int staffId)
    {
        await _requestStatusService.MarkAsStartedAsync(requestId, staffId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/in-progress/staff/{staffId:int}")]
    public async Task<IActionResult> MarkAsInProgress(int requestId, int staffId)
    {
        await _requestStatusService.MarkAsInProgressAsync(requestId, staffId);
        return NoContent();
    }

    [HttpPut("{requestId:int}/complete/staff/{staffId:int}")]
    public async Task<IActionResult> MarkAsCompleted(int requestId, int staffId)
    {
        await _requestStatusService.MarkAsCompletedAsync(requestId, staffId);
        return NoContent();
    }

    private static string SanitizeFileBaseName(string originalBaseName)
    {
        var lowered = originalBaseName.ToLowerInvariant();
        var replaced = Regex.Replace(lowered, "[^a-z0-9]+", "-");
        var trimmed = replaced.Trim('-');
        return string.IsNullOrWhiteSpace(trimmed) ? "file" : trimmed;
    }

    private static string GenerateUniqueFileName(string directoryPath, string safeBaseName, string extension)
    {
        while (true)
        {
            var randomKey = Path.GetRandomFileName().Replace(".", string.Empty)[..8].ToLowerInvariant();
            var candidateName = $"{safeBaseName}-{randomKey}{extension}";
            var candidatePath = Path.Combine(directoryPath, candidateName);
            if (!System.IO.File.Exists(candidatePath))
                return candidateName;
        }
    }
}