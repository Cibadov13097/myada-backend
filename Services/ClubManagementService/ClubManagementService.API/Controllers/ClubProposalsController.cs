using Asp.Versioning;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-proposals")]
[ApiController]
[Authorize]
[Tags("Club Proposals")]
public class ClubProposalsController : ControllerBase
{
    private readonly IClubNotificationService _notificationService;
    private readonly IImageStorageService _imageStorageService;

    public ClubProposalsController(
        IClubNotificationService notificationService,
        IImageStorageService imageStorageService)
    {
        _notificationService = notificationService;
        _imageStorageService = imageStorageService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] ClubProposalRequest request)
    {
        var logoUrl = await UploadFileIfPresentAsync("club-proposals/logos", request.LogoFile);
        if (logoUrl == string.Empty)
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Unsupported logo file type." });

        var constitutionUrl = await UploadFileIfPresentAsync("club-proposals/constitutions", request.ConstitutionFile);
        if (constitutionUrl == string.Empty)
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Unsupported constitution file type." });

        var proposalId = $"PROP-{DateTime.UtcNow.Year}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Club Proposal Submitted",
            Message = $"Club proposal '{request.Name ?? "Untitled"}' was submitted.",
            Source = "club-management",
            Action = "club_proposal.created",
            EntityType = "club_proposal",
            EntityId = proposalId,
            ActorUserId = GetCurrentUserIdOrDefault()
        });

        return Ok(new
        {
            proposalId,
            status = "pending_review",
            logoUrl,
            constitutionUrl
        });
    }

    private async Task<string?> UploadFileIfPresentAsync(string directory, IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        if (!_imageStorageService.IsSupportedImage(file.FileName))
            return string.Empty;

        await using var stream = file.OpenReadStream();
        return await _imageStorageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            directory,
            HttpContext.RequestAborted);
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class ClubProposalRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? ConstitutionFile { get; set; }
    }
}
