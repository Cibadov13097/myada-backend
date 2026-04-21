using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public NotificationsController(
        NotificationDbContext dbContext,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
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

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmail.Trim();
        var subject = request.Subject.Trim();
        var message = request.Message.Trim();

        if (toEmail.Length == 0 || subject.Length == 0 || message.Length == 0)
        {
            return BadRequest(new
            {
                error = "ToEmail, subject and message are required."
            });
        }

        await _notificationService.SendEmailAsync(
            toEmail,
            subject,
            message,
            cancellationToken);

        _dbContext.Notifications.Add(new Notification
        {
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Direct Email" : request.Type.Trim(),
            Message = message,
            NotificationType = NotificationType.Email,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Email notification sent successfully."
        });
    }

    public sealed class NotificationItemDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SendEmailNotificationRequest
    {
        public string? Type { get; set; }

        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;
    }
}
