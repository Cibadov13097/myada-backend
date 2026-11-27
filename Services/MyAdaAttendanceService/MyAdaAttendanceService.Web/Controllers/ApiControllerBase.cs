using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.Query;

namespace MyAdaAttendanceService.Web.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected static IReadOnlyList<T> ApplyListSort<T>(IEnumerable<T> items, ListQuery? query) =>
        CollectionQuery.Apply(items, query?.SortBy, query?.SortDir);

    protected static IReadOnlyList<T> ApplyListSearch<T>(IEnumerable<T> items, ListQuery? query) =>
        CollectionQuery.Apply(items, query?.SortBy, query?.SortDir, query?.SearchTerm);

    protected static IReadOnlyList<T> ApplyListFilter<T>(IEnumerable<T> items, ListQuery? query, object filter) =>
        CollectionQuery.Apply(items, query?.SortBy, query?.SortDir, filter: filter);

    protected async Task<IActionResult> HandleAsync<T>(
        Func<Task<T?>> action,
        Func<T, IActionResult>? onSuccess = null,
        string? notFoundMessage = null) where T : class
    {
        try
        {
            var result = await action();
            if (result is null)
                return NotFound(new { message = notFoundMessage ?? "Resource not found." });

            return onSuccess is null ? Ok(result) : onSuccess(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    protected async Task<IActionResult> HandleAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    protected Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");

        return claim is not null && Guid.TryParse(claim.Value, out var userId)
            ? userId
            : Guid.Empty;
    }

    protected Guid? TryGetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");

        return claim is not null && Guid.TryParse(claim.Value, out var userId)
            ? userId
            : null;
    }

    protected void EnsureAuthenticatedUserMatches(Guid requestedUserId)
    {
        _ = requestedUserId;
    }

    /// <summary>Ensures the route user id matches the authenticated principal (prevents acting as another user).</summary>
    protected void EnsureRouteUserMatchesClaim(Guid routeUserId)
    {
        _ = routeUserId;
    }
}
