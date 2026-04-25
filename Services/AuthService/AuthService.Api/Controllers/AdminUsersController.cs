using System.Security.Claims;
using AuthService.API.Integrations;
using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/admin/users")]
[Authorize(Roles = "admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly MyAdaDbContext _dbContext;
    private readonly SupportStaffProvisioningClient _supportStaffProvisioningClient;

    public AdminUsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        MyAdaDbContext dbContext,
        SupportStaffProvisioningClient supportStaffProvisioningClient)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _supportStaffProvisioningClient = supportStaffProvisioningClient;
    }

    [HttpGet]
    public async Task<ActionResult<AdminUserListResponseDto>> GetAll(
        [FromQuery] string? role = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        string? roleId = null;
        var normalizedRole = NormalizeRoleName(role);
        if (!string.IsNullOrWhiteSpace(normalizedRole))
        {
            var existingRole = await _roleManager.FindByNameAsync(normalizedRole);
            if (existingRole is null)
                return BadRequest($"Role '{role}' does not exist");

            roleId = existingRole.Id;
        }

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(user =>
                (user.UserName != null && user.UserName.ToLower().Contains(searchTerm)) ||
                (user.Email != null && user.Email.ToLower().Contains(searchTerm)) ||
                user.FirstName.ToLower().Contains(searchTerm) ||
                user.LastName.ToLower().Contains(searchTerm) ||
                user.OrganizationalId.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(roleId))
        {
            query = query.Where(user =>
                _dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleId));
        }

        var users = await query
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        var roleLookup = await GetRoleLookupAsync(users.Select(user => user.Id), cancellationToken);

        return Ok(new AdminUserListResponseDto
        {
            Count = users.Count,
            Users = users
                .Select(user => MapUser(user, roleLookup.GetValueOrDefault(user.Id) ?? Array.Empty<string>()))
                .ToList()
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDto>> GetById(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("User id is required");

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (user == null)
            return NotFound("User not found");

        return Ok(MapUser(user, await GetRolesForUserAsync(user.Id, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateUserWithRoleDto dto, CancellationToken cancellationToken = default)
    {
        var roleName = NormalizeRoleName(dto.Role);
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role is required");

        if (!await _roleManager.RoleExistsAsync(roleName))
            return BadRequest($"Role '{dto.Role}' does not exist");

        var email = dto.Email.Trim();
        var username = dto.Username.Trim();
        var organizationalId = dto.OrganizationalId.Trim();

        if (await EmailInUseAsync(email, excludedUserId: null, cancellationToken))
            return BadRequest("Email is already in use");

        if (await UsernameInUseAsync(username, excludedUserId: null, cancellationToken))
            return BadRequest("Username is already in use");

        if (await OrganizationalIdInUseAsync(organizationalId, excludedUserId: null, cancellationToken))
            return BadRequest("OrganizationalId is already in use");

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            OrganizationalId = organizationalId,
            PhoneNumber = TrimToNull(dto.PhoneNumber),
            ProfileImage = TrimToNull(dto.ProfileImage),
            UserType = MapUserType(roleName),
            Status = Status.Active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return BadRequest(createResult.Errors);
        }

        var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return BadRequest(addToRoleResult.Errors);
        }

        var supportProvisionResult = await _supportStaffProvisioningClient
            .ProvisionForRoleAsync(user.Id, roleName, cancellationToken);

        if (!supportProvisionResult.IsSuccess)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = supportProvisionResult.ErrorMessage ?? "Failed to provision support staff status."
            });
        }

        await CommitTransactionAsync(transaction, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapUser(user, new[] { roleName }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminUserDto>> Update(string id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("User id is required");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsDeleted)
            return NotFound("User not found");

        var roleName = NormalizeRoleName(dto.Role);
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role is required");

        if (!await _roleManager.RoleExistsAsync(roleName))
            return BadRequest($"Role '{dto.Role}' does not exist");

        var email = dto.Email.Trim();
        var username = dto.Username.Trim();
        var organizationalId = dto.OrganizationalId.Trim();

        if (await EmailInUseAsync(email, user.Id, cancellationToken))
            return BadRequest("Email is already in use");

        if (await UsernameInUseAsync(username, user.Id, cancellationToken))
            return BadRequest("Username is already in use");

        if (await OrganizationalIdInUseAsync(organizationalId, user.Id, cancellationToken))
            return BadRequest("OrganizationalId is already in use");

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);

        user.UserName = username;
        user.Email = email;
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.OrganizationalId = organizationalId;
        user.PhoneNumber = TrimToNull(dto.PhoneNumber);
        user.ProfileImage = TrimToNull(dto.ProfileImage);
        user.UserType = MapUserType(roleName);
        user.Status = dto.Status ?? Status.Active;
        user.EmailConfirmed = true;
        user.ModifiedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return BadRequest(updateResult.Errors);
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.Password.Trim());
            if (!resetPasswordResult.Succeeded)
            {
                await RollbackTransactionAsync(transaction, cancellationToken);
                return BadRequest(resetPasswordResult.Errors);
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!(currentRoles.Count == 1 && string.Equals(currentRoles[0], roleName, StringComparison.OrdinalIgnoreCase)))
        {
            if (currentRoles.Count > 0)
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    await RollbackTransactionAsync(transaction, cancellationToken);
                    return BadRequest(removeRolesResult.Errors);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addRoleResult.Succeeded)
            {
                await RollbackTransactionAsync(transaction, cancellationToken);
                return BadRequest(addRoleResult.Errors);
            }
        }

        var supportProvisionResult = await _supportStaffProvisioningClient
            .ProvisionForRoleAsync(user.Id, roleName, cancellationToken);

        if (!supportProvisionResult.IsSuccess)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = supportProvisionResult.ErrorMessage ?? "Failed to provision support staff status."
            });
        }

        await CommitTransactionAsync(transaction, cancellationToken);

        return Ok(MapUser(user, new[] { roleName }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("User id is required");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(currentUserId) &&
            string.Equals(currentUserId, id, StringComparison.Ordinal))
        {
            return BadRequest("Admin users cannot delete their own account.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsDeleted)
            return NotFound("User not found");

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            await RollbackTransactionAsync(transaction, cancellationToken);
            return BadRequest(deleteResult.Errors);
        }

        await CommitTransactionAsync(transaction, cancellationToken);
        return NoContent();
    }

    private async Task<bool> EmailInUseAsync(string email, string? excludedUserId, CancellationToken cancellationToken)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                !user.IsDeleted &&
                user.Id != excludedUserId &&
                user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    private async Task<bool> UsernameInUseAsync(string username, string? excludedUserId, CancellationToken cancellationToken)
    {
        var normalizedUsername = _userManager.NormalizeName(username);
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                !user.IsDeleted &&
                user.Id != excludedUserId &&
                user.NormalizedUserName == normalizedUsername,
                cancellationToken);
    }

    private async Task<bool> OrganizationalIdInUseAsync(string organizationalId, string? excludedUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                !user.IsDeleted &&
                user.Id != excludedUserId &&
                user.OrganizationalId == organizationalId,
                cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> GetRolesForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var lookup = await GetRoleLookupAsync(new[] { userId }, cancellationToken);
        return lookup.GetValueOrDefault(userId) ?? Array.Empty<string>();
    }

    private async Task<Dictionary<string, IReadOnlyCollection<string>>> GetRoleLookupAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        var rows = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            orderby role.Name
            select new
            {
                userRole.UserId,
                RoleName = role.Name ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.UserId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(item => item.RoleName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static AdminUserDto MapUser(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        return new AdminUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationalId = user.OrganizationalId,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            UserType = user.UserType,
            Status = user.Status,
            Roles = roles,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        };
    }

    private static UserType MapUserType(string roleName)
    {
        return roleName switch
        {
            "student" => UserType.Student,
            "instructor" => UserType.Faculty,
            "dispatcher" => UserType.Staff,
            _ => UserType.Staff
        };
    }

    private static string NormalizeRoleName(string? roleName)
    {
        return roleName?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
            return null;

        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static async Task RollbackTransactionAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction != null)
            await transaction.RollbackAsync(cancellationToken);
    }

    private static async Task CommitTransactionAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
    }
}
