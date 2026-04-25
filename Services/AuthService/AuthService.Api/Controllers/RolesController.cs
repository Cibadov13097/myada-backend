using System.Text.RegularExpressions;
using AuthService.Application.DTOs.Roles;
using AuthService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/roles")]
[Authorize(Roles = "admin")]
public class RolesController : ControllerBase
{
    private static readonly Regex RoleNamePattern = new("^[a-z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly MyAdaDbContext _dbContext;

    public RolesController(RoleManager<IdentityRole> roleManager, MyAdaDbContext dbContext)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var userCounts = await _dbContext.UserRoles
            .AsNoTracking()
            .GroupBy(userRole => userRole.RoleId)
            .Select(group => new
            {
                RoleId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);

        return Ok(roles.Select(role => MapRole(role, userCounts.GetValueOrDefault(role.Id))).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetById(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Role id is required");

        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (role == null)
            return NotFound("Role not found");

        return Ok(MapRole(role, await GetUserCountAsync(role.Id, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto, CancellationToken cancellationToken)
    {
        var normalizedRoleName = NormalizeRoleName(dto.Name);
        var validationError = ValidateRoleName(normalizedRoleName);
        if (validationError != null)
            return BadRequest(validationError);

        var normalizedLookup = _roleManager.NormalizeKey(normalizedRoleName);
        var roleExists = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(role => role.NormalizedName == normalizedLookup, cancellationToken);

        if (roleExists)
            return Conflict(new { message = $"Role '{normalizedRoleName}' already exists." });

        var role = new IdentityRole(normalizedRoleName);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, MapRole(role, 0));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDto>> Update(string id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Role id is required");

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound("Role not found");

        var normalizedRoleName = NormalizeRoleName(dto.Name);
        var validationError = ValidateRoleName(normalizedRoleName);
        if (validationError != null)
            return BadRequest(validationError);

        var normalizedLookup = _roleManager.NormalizeKey(normalizedRoleName);
        var nameInUse = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(existingRole =>
                existingRole.Id != id &&
                existingRole.NormalizedName == normalizedLookup,
                cancellationToken);

        if (nameInUse)
            return Conflict(new { message = $"Role '{normalizedRoleName}' already exists." });

        role.Name = normalizedRoleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(MapRole(role, await GetUserCountAsync(role.Id, cancellationToken)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Role id is required");

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound("Role not found");

        var userCount = await GetUserCountAsync(role.Id, cancellationToken);
        if (userCount > 0)
        {
            return BadRequest(new
            {
                message = $"Role '{role.Name ?? role.Id}' is assigned to {userCount} user(s) and cannot be deleted."
            });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    private async Task<int> GetUserCountAsync(string roleId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .CountAsync(userRole => userRole.RoleId == roleId, cancellationToken);
    }

    private static string NormalizeRoleName(string? roleName)
    {
        return roleName?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string? ValidateRoleName(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return "Role name is required.";

        if (!RoleNamePattern.IsMatch(roleName))
            return "Role name may contain only lowercase letters, numbers, and underscores.";

        return null;
    }

    private static RoleDto MapRole(IdentityRole role, int userCount)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            NormalizedName = role.NormalizedName,
            UserCount = userCount
        };
    }
}
