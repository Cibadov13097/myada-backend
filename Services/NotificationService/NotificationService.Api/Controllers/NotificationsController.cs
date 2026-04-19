using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;

    public NotificationsController(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string type = "all",
        [FromQuery] string channel = "all",
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        var query = _dbContext.Notifications.AsNoTracking().AsQueryable();

        if (!string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => EF.Functions.ILike(n.Type, $"%{type}%"));
        }

        if (!string.Equals(channel, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<NotificationType>(channel, true, out var parsedChannel))
            {
                return BadRequest(new
                {
                    error = "Invalid channel value. Allowed: all, Email, Push, Sms"
                });
            }

            query = query.Where(n => n.NotificationType == parsedChannel);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                Channel = n.NotificationType.ToString(),
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            limit
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var notification = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                Channel = n.NotificationType.ToString(),
                CreatedAt = n.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (notification is null)
            return NotFound();

        return Ok(notification);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        if (notification is null)
            return NotFound();

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    public sealed class NotificationItemDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
