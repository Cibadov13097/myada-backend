using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.API.Security;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-memberships")]
[ApiController]
[Authorize]
[Tags("Club Memberships")]
public class ClubMembershipsController : ControllerBase
{
    private readonly IClubMembershipRepository _membershipRepository;
    private readonly IClubRepository _clubRepository;

    public ClubMembershipsController(IClubMembershipRepository membershipRepository, IClubRepository clubRepository)
    {
        _membershipRepository = membershipRepository;
        _clubRepository = clubRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clubId = null, [FromQuery] Guid? userId = null)
    {
        var currentUserId = GetCurrentUserIdOrDefault();
        if (currentUserId == Guid.Empty)
            return Unauthorized();

        var memberships = await _membershipRepository.GetAllAsync();
        var filtered = memberships.AsEnumerable();

        if (clubId is > 0)
            filtered = filtered.Where(m => m.ClubId == clubId.Value);
        if (userId.HasValue)
            filtered = filtered.Where(m => m.UserId == userId.Value);

        if (!User.IsMyAdaAdministrator())
            filtered = filtered.Where(m => m.UserId == currentUserId);

        return Ok(filtered.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entity = await _membershipRepository.GetByIdAsync(id);
        if (entity == null)
            return NotFound();

        var denied = EnsureCanAccess(entity);
        if (denied != null)
            return denied;

        return Ok(Map(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClubMembershipRequest request)
    {
        var currentUserId = GetCurrentUserIdOrDefault();
        if (currentUserId == Guid.Empty)
            return Unauthorized();

        var club = await _clubRepository.GetByIdAsync(request.ClubId);
        if (club == null)
            return NotFound(new { message = $"Club {request.ClubId} not found." });

        var clubIds = await _membershipRepository.GetClubIdsByUserAsync(currentUserId, HttpContext.RequestAborted);
        if (clubIds.Contains(request.ClubId))
            return Conflict(new { message = "You are already a member of this club." });

        var entity = new ClubMembership
        {
            ClubId = request.ClubId,
            UserId = currentUserId,
            CreatedByUserId = currentUserId
        };

        await _membershipRepository.AddAsync(entity);
        await _membershipRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), id = entity.Id }, Map(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClubMembershipRequest request)
    {
        var entity = await _membershipRepository.GetByIdAsync(id);
        if (entity == null)
            return NotFound();

        var denied = EnsureCanAccess(entity);
        if (denied != null)
            return denied;

        var club = await _clubRepository.GetByIdAsync(request.ClubId);
        if (club == null)
            return NotFound(new { message = $"Club {request.ClubId} not found." });

        if (entity.ClubId != request.ClubId)
        {
            var clubIds = await _membershipRepository.GetClubIdsByUserAsync(entity.UserId, HttpContext.RequestAborted);
            if (clubIds.Contains(request.ClubId))
                return Conflict(new { message = "Membership already exists for this club and user." });
        }

        var currentUserId = GetCurrentUserIdOrDefault();
        entity.ClubId = request.ClubId;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedByUserId = currentUserId == Guid.Empty ? null : currentUserId;

        await _membershipRepository.UpdateAsync(entity);
        await _membershipRepository.SaveChangesAsync();

        return Ok(Map(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _membershipRepository.GetByIdAsync(id);
        if (entity == null)
            return NotFound();

        var denied = EnsureCanAccess(entity);
        if (denied != null)
            return denied;

        await _membershipRepository.DeleteAsync(entity);
        await _membershipRepository.SaveChangesAsync();
        return NoContent();
    }

    private IActionResult? EnsureCanAccess(ClubMembership entity)
    {
        if (User.IsMyAdaAdministrator())
            return null;

        var currentUserId = GetCurrentUserIdOrDefault();
        if (currentUserId == Guid.Empty)
            return Unauthorized();

        if (entity.UserId != currentUserId)
            return Forbid();

        return null;
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private static object Map(ClubMembership m) => new
    {
        m.Id,
        m.ClubId,
        m.UserId,
        m.CreatedAt,
        m.ModifiedAt
    };

    public sealed class CreateClubMembershipRequest
    {
        public int ClubId { get; set; }
    }

    public sealed class UpdateClubMembershipRequest
    {
        public int ClubId { get; set; }
    }
}
