using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.API.Responses;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Infrastructure.Services;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/clubs")]
[ApiController]
[Authorize]
[Tags("Clubs")]
public class ClubsController : ControllerBase
{
    private readonly IClubService _service;
    private readonly IClubNotificationService _notificationService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClubAdminWorkspaceStore _workspace;
    private readonly IClubMembershipRepository _clubMembershipRepository;
    private readonly ClubManagementDbContext _db;

    public ClubsController(
        IClubService service,
        IClubNotificationService notificationService,
        IImageStorageService imageStorageService,
        ClubAdminWorkspaceStore workspace,
        IClubMembershipRepository clubMembershipRepository,
        ClubManagementDbContext db)
    {
        _service = service;
        _notificationService = notificationService;
        _imageStorageService = imageStorageService;
        _workspace = workspace;
        _clubMembershipRepository = clubMembershipRepository;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var clubs = await _service.GetAllAsync();
        var filtered = clubs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(c => string.Equals(c.CategoryName, category, StringComparison.OrdinalIgnoreCase));

        var total = filtered.Count();
        var items = filtered.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1)).Select(c =>
        {
            var profileImageUrl = ClubImageResponse.Normalize(c.ProfileImageUrl);
            var backgroundImageUrl = ClubImageResponse.Normalize(c.BackgroundImageUrl);
            var backgroundImage = ClubImageResponse.ResolveDisplayImage(backgroundImageUrl);
            return new
            {
                profileImageUrl,
                backgroundImageUrl,
                id = c.Id,
                name = c.Name,
                category = c.CategoryName ?? "General",
                backgroundImage,
                image = backgroundImage,
                tags = Array.Empty<string>(),
                members = 0,
                about = c.Description,
                status = c.Status.ToString()
            };
        });

        return Ok(new { items, total, page, limit });
    }

    /// <summary>Public club detail including <c>focusAreas</c> (0–6 items) and social links when stored.</summary>
    [HttpGet("{clubId:int}")]
    public async Task<IActionResult> GetById(int clubId, CancellationToken cancellationToken)
    {
        var dto = await _service.GetByIdAsync(clubId);
        if (dto == null)
            return NotFound();

        var employees = await (
            from e in _db.ClubEmployees.AsNoTracking()
            where e.ClubId == clubId
            join p in _db.ClubPositions.AsNoTracking() on e.PositionId equals (int?)p.Id into positions
            from p in positions.DefaultIfEmpty()
            orderby e.Id
            select new
            {
                id = e.Id.ToString(),
                userId = e.UserId.ToString("D"),
                positionId = e.PositionId == null ? null : e.PositionId.Value.ToString(),
                positionTitle = p == null ? null : p.Title,
                positionName = p == null ? null : p.Title
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(ToPublicClubDetail(dto, employees));
    }

    private static object ToPublicClubDetail(ClubDto dto, IReadOnlyList<object> officers)
    {
        var links = dto.SocialLinks;
        var resources = dto.Resources ?? new List<ClubResourceItemDto>();
        var profileImageUrl = ClubImageResponse.Normalize(dto.ProfileImageUrl);
        var backgroundImageUrl = ClubImageResponse.Normalize(dto.BackgroundImageUrl);
        var backgroundImage = ClubImageResponse.ResolveDisplayImage(backgroundImageUrl);
        var hasSocial = links != null && (
            !string.IsNullOrWhiteSpace(links.Website) ||
            !string.IsNullOrWhiteSpace(links.Instagram) ||
            !string.IsNullOrWhiteSpace(links.X) ||
            !string.IsNullOrWhiteSpace(links.Tiktok));
        var hasResources = resources.Count > 0;

        return new
        {
            id = dto.Id,
            name = dto.Name,
            category = dto.CategoryName ?? "General",
            profileImageUrl,
            backgroundImageUrl,
            backgroundImage,
            image = backgroundImage,
            about = dto.Description,
            focusAreas = dto.FocusAreas,
            officers,
            memberProfiles = Array.Empty<object>(),
            announcements = Array.Empty<object>(),
            email = dto.Email,
            website = string.IsNullOrWhiteSpace(links?.Website) ? null : links.Website,
            instagram = string.IsNullOrWhiteSpace(links?.Instagram) ? null : links.Instagram,
            x = string.IsNullOrWhiteSpace(links?.X) ? null : links.X,
            twitter = string.IsNullOrWhiteSpace(links?.X) ? null : links.X,
            tiktok = string.IsNullOrWhiteSpace(links?.Tiktok) ? null : links.Tiktok,
            socialLinks = hasSocial ? links : null,
            resources = hasResources ? resources : null,
            documents = hasResources ? resources : null
        };
    }

    [HttpGet("{clubId:int}/members")]
    public async Task<IActionResult> GetMembers(int clubId, CancellationToken cancellationToken)
    {
        var userIds = await _clubMembershipRepository.GetUserIdsByClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        var items = userIds.Select(u => new { userId = u.ToString("D") }).ToList();
        return Ok(new { items });
    }

    [HttpPost("{clubId:int}/join-applications")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> JoinApplication(int clubId, [FromForm] JoinApplicationRequest request)
    {
        var membershipId = Guid.NewGuid();
        var applicantUserId = request.UserId?.Trim();
        if (string.IsNullOrWhiteSpace(applicantUserId))
        {
            // Back-compat: if not provided, fall back to JWT subject.
            applicantUserId = GetCurrentUserIdOrDefault().ToString("D");
        }

        var portfolioUrls = await UploadPortfolioFilesAsync(clubId, membershipId.ToString("D"), request.PortfolioFiles);
        if (portfolioUrls == null)
        {
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Unsupported portfolio file type." });
        }

        // Store application for club-admin views (in-memory workspace store).
        var ws = _workspace.GetOrCreate(clubId);
        ws.MembershipApplications[membershipId] = new MembershipApplicationRow
        {
            Id = membershipId,
            ApplicantUserId = applicantUserId,
            LetterOfPurpose = request.LetterOfPurpose,
            Links = request.PortfolioLinks,
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _notificationService.PublishAsync(new NotificationQueueMessage
        {
            Type = "Membership Application Submitted",
            Message = $"A membership application was submitted for club '{clubId}'.",
            Source = "club-management",
            Action = "membership_application.created",
            EntityType = "membership_application",
            EntityId = membershipId.ToString("D"),
            ActorUserId = Guid.TryParse(applicantUserId, out var gid) ? gid : GetCurrentUserIdOrDefault()
        });

        return Ok(new
        {
            applicationId = $"MEM-{membershipId:D}",
            status = "Submitted",
            submittedAt = DateTime.UtcNow,
            portfolioUrls
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClubDto dto)
    {
        var approvedBy = GetCurrentUserIdOrDefault();
        if (approvedBy == Guid.Empty)
            return Unauthorized();

        dto.ApprovedBy = approvedBy;
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), clubId = created.Id }, created);
    }

    [HttpPatch("{clubId:int}")]
    public async Task<IActionResult> Update(int clubId, [FromBody] UpdateClubDto dto)
    {
        var updated = await _service.UpdateAsync(clubId, dto);
        return Ok(updated);
    }

    [HttpDelete("{clubId:int}")]
    public async Task<IActionResult> Delete(int clubId)
    {
        await _service.DeleteAsync(clubId);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private async Task<List<string>?> UploadPortfolioFilesAsync(int clubId, string applicationId, List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
            return new List<string>();

        var uploaded = new List<string>(files.Count);
        foreach (var file in files.Where(f => f is { Length: > 0 }))
        {
            if (!_imageStorageService.IsSupportedImage(file.FileName))
                return null;

            await using var stream = file.OpenReadStream();
            var url = await _imageStorageService.UploadImageAsync(
                stream,
                file.FileName,
                file.ContentType,
                $"clubs/{clubId}/membership-applications/{applicationId}",
                HttpContext.RequestAborted);
            uploaded.Add(url);
        }

        return uploaded;
    }

    public sealed class JoinApplicationRequest
    {
        public string? UserId { get; set; }
        public string LetterOfPurpose { get; set; } = string.Empty;
        public string? PortfolioLinks { get; set; }
        public List<IFormFile>? PortfolioFiles { get; set; }
    }
}
