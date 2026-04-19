using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/club-proposals")]
[ApiController]
[Authorize]
[Tags("Club Proposals")]
public class ClubProposalsController : ControllerBase
{
    private readonly IClubNotificationService _notificationService;

    public ClubProposalsController(IClubNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] ClubProposalRequest request)
    {
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
            status = "pending_review"
        });
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
