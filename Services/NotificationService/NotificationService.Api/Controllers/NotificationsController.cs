using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationDbContext dbContext,
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] string type = "all",
        [FromQuery] string channel = "all",
        [FromQuery] Guid? recipientUserId = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        var query = ApplyVisibleNotificationsScope(userId);

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

        if (recipientUserId.HasValue && recipientUserId.Value != Guid.Empty)
        {
            query = query.Where(n => n.RecipientUserId == recipientUserId.Value);
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
                RecipientUserId = n.RecipientUserId,
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
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var notification = await ApplyVisibleNotificationsScope(userId)
            .Where(n => n.Id == id)
            .Select(n => new NotificationItemDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                Channel = n.NotificationType.ToString(),
                RecipientUserId = n.RecipientUserId,
                CreatedAt = n.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (notification is null)
            return NotFound();

        return Ok(notification);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);
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

        try
        {
            await _notificationService.SendEmailAsync(
                toEmail,
                subject,
                message,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Email send failed due to invalid SMTP configuration for recipient {Recipient}.",
                toEmail);

            return Problem(
                title: "Email delivery is not configured",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "SMTP rejected email delivery to {Recipient}. Verify the SMTP username, app password, and sender address.",
                toEmail);

            return Problem(
                title: "Email delivery failed",
                detail: "The SMTP server rejected the message. Verify the SMTP username, app password, and sender address.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        _dbContext.Notifications.Add(new Notification
        {
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Direct Email" : request.Type.Trim(),
            Message = message,
            NotificationType = NotificationType.Email,
            CreatedAt = DateTime.UtcNow,
            RecipientUserId = request.RecipientUserId ?? GetCurrentUserIdOrNull()
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
        public Guid? RecipientUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SendEmailNotificationRequest
    {
        public string? Type { get; set; }
        public Guid? RecipientUserId { get; set; }

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

    private IQueryable<Notification> ApplyVisibleNotificationsScope(Guid userId)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId || n.RecipientUserId == null);
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private Guid? GetCurrentUserIdOrNull()
    {
        var userId = GetCurrentUserIdOrDefault();
        return userId == Guid.Empty ? null : userId;
    }
}
