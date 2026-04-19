using Asp.Versioning;
using ClubManagementService.API.Security;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users/{userId:guid}")]
[ApiController]
[Authorize]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IEventRegistrationService _eventRegistrationService;
    private readonly IClubMembershipRepository _clubMembershipRepository;

    public UsersController(
        IJobApplicationService jobApplicationService,
        IEventRegistrationService eventRegistrationService,
        IClubMembershipRepository clubMembershipRepository)
    {
        _jobApplicationService = jobApplicationService;
        _eventRegistrationService = eventRegistrationService;
        _clubMembershipRepository = clubMembershipRepository;
    }

    [HttpGet("club-memberships")]
    public async Task<IActionResult> GetClubMemberships(Guid userId, CancellationToken cancellationToken)
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        var clubIds = await _clubMembershipRepository.GetClubIdsByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var items = clubIds.Select(id => new { clubId = id, role = "member" }).ToList();
        return Ok(new { items });
    }

    [HttpGet("vacancy-applications")]
    public async Task<IActionResult> GetVacancyApplications(Guid userId, [FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        var all = await _jobApplicationService.GetMyVacancyApplicationsAsync(userId);
        var total = all.Count;
        var items = all.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1)).ToList();
        return Ok(new { items, page, limit, total });
    }

    [HttpGet("membership-applications")]
    public IActionResult GetMembershipApplications(Guid userId)
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        return Ok(new { items = Array.Empty<object>() });
    }

    [HttpGet("event-registrations")]
    public async Task<IActionResult> GetEventRegistrations(Guid userId)
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        var mine = (await _eventRegistrationService.GetAllAsync()).Where(a => a.UserId == userId);
        return Ok(new { items = mine });
    }

    [HttpGet("club-notifications")]
    public IActionResult GetClubNotifications(Guid userId, [FromQuery] string type = "all")
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        _ = type;
        return Ok(new { items = Array.Empty<object>() });
    }

    [HttpPatch("club-notifications/{notificationId}/read")]
    public IActionResult MarkNotificationAsRead(Guid userId, string notificationId)
    {
        var denied = EnsureCallerMatchesUser(userId);
        if (denied != null)
            return denied;

        return Ok(new { notificationId, read = true });
    }

    /// <summary>Ensures the route <paramref name="userId"/> matches the authenticated subject (JWT <c>sub</c> or <c>user_id</c>).</summary>
    private IActionResult? EnsureCallerMatchesUser(Guid userId)
    {
        if (User.IsMyAdaAdministrator())
            return null;

        var principalId = GetCurrentUserIdOrDefault();
        if (principalId == Guid.Empty)
            return Unauthorized();
        if (principalId != userId)
            return Forbid();
        return null;
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }
}
