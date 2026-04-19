using Asp.Versioning;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/me")]
[ApiController]
[Authorize]
[Tags("Me")]
public class MeController : ControllerBase
{
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IEventRegistrationService _eventRegistrationService;
    private readonly IClubMembershipRepository _clubMembershipRepository;

    public MeController(
        IJobApplicationService jobApplicationService,
        IEventRegistrationService eventRegistrationService,
        IClubMembershipRepository clubMembershipRepository)
    {
        _jobApplicationService = jobApplicationService;
        _eventRegistrationService = eventRegistrationService;
        _clubMembershipRepository = clubMembershipRepository;
    }

    [HttpGet("club-memberships")]
    public async Task<IActionResult> GetClubMemberships(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var clubIds = await _clubMembershipRepository.GetClubIdsByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var items = clubIds.Select(id => new { clubId = id, role = "member" }).ToList();
        return Ok(new { items });
    }

    [HttpGet("vacancy-applications")]
    public async Task<IActionResult> GetVacancyApplications([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserIdOrDefault();
        var all = await _jobApplicationService.GetMyVacancyApplicationsAsync(userId);
        var total = all.Count;
        var items = all.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1)).ToList();
        return Ok(new { items, page, limit, total });
    }

    [HttpGet("membership-applications")]
    public IActionResult GetMembershipApplications() => Ok(new { items = Array.Empty<object>() });

    [HttpGet("event-registrations")]
    public async Task<IActionResult> GetEventRegistrations()
    {
        var userId = GetCurrentUserIdOrDefault();
        var mine = (await _eventRegistrationService.GetAllAsync()).Where(a => a.UserId == userId);
        return Ok(new { items = mine });
    }

    [HttpGet("club-notifications")]
    public IActionResult GetClubNotifications([FromQuery] string type = "all") => Ok(new { items = Array.Empty<object>() });

    [HttpPatch("club-notifications/{notificationId}/read")]
    public IActionResult MarkNotificationAsRead(string notificationId) => Ok(new { notificationId, read = true });

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }
}
