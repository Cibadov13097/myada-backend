using System.Collections.Concurrent;
using System.Globalization;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.API.Background;

public sealed class ClubNotificationReminderHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReminderTolerance = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SentKeyRetention = TimeSpan.FromDays(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClubNotificationReminderHostedService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _sentReminderKeys = new();

    public ClubNotificationReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ClubNotificationReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed while processing scheduled club notifications.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubManagementDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IClubNotificationService>();
        var nowUtc = DateTime.UtcNow;

        await PublishEventRemindersAsync(
            db,
            notificationService,
            nowUtc,
            TimeSpan.FromHours(24),
            "event.reminder_24h",
            cancellationToken).ConfigureAwait(false);

        await PublishEventRemindersAsync(
            db,
            notificationService,
            nowUtc,
            TimeSpan.FromHours(3),
            "event.reminder_3h",
            cancellationToken).ConfigureAwait(false);

        await CloseExpiredVacanciesAsync(db, notificationService, nowUtc, cancellationToken).ConfigureAwait(false);
        CleanupSentReminderKeys(nowUtc);
    }

    private async Task PublishEventRemindersAsync(
        ClubManagementDbContext db,
        IClubNotificationService notificationService,
        DateTime nowUtc,
        TimeSpan targetLeadTime,
        string action,
        CancellationToken cancellationToken)
    {
        var windowStart = nowUtc.Add(targetLeadTime).Subtract(ReminderTolerance);
        var windowEnd = nowUtc.Add(targetLeadTime).Add(ReminderTolerance);

        var dueRegistrations = await db.EventRegistrations
            .AsNoTracking()
            .Select(r => new
            {
                r.EventId,
                r.UserId,
                EventName = r.Event != null && !string.IsNullOrWhiteSpace(r.Event.Name) ? r.Event.Name : r.EventNameSnapshot,
                EventStartTime = r.Event != null ? r.Event.StartTime : r.EventStartTimeSnapshot
            })
            .Where(r => r.UserId != Guid.Empty && r.EventStartTime >= windowStart && r.EventStartTime < windowEnd)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var registration in dueRegistrations)
        {
            var eventStartUtc = NormalizeUtc(registration.EventStartTime);
            var reminderKey = string.Create(
                CultureInfo.InvariantCulture,
                $"{action}:{registration.EventId}:{registration.UserId:D}:{eventStartUtc.Ticks}");

            if (!_sentReminderKeys.TryAdd(reminderKey, nowUtc))
                continue;

            await notificationService.PublishAsync(new NotificationQueueMessage
            {
                Type = string.IsNullOrWhiteSpace(registration.EventName)
                    ? $"Event {registration.EventId.ToString(CultureInfo.InvariantCulture)}"
                    : registration.EventName,
                Message = FormatEventDateTime(eventStartUtc),
                Channel = "Push",
                Source = "club-management",
                Action = action,
                EntityType = "event",
                EntityId = registration.EventId.ToString(CultureInfo.InvariantCulture),
                RecipientUserId = registration.UserId
            }).ConfigureAwait(false);
        }
    }

    private async Task CloseExpiredVacanciesAsync(
        ClubManagementDbContext db,
        IClubNotificationService notificationService,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var expiredVacancies = await db.JobVacancies
            .Include(v => v.ClubPosition)
            .Where(v =>
                !v.IsDraft &&
                v.IsActive &&
                v.ApplicationDeadline.HasValue &&
                v.ApplicationDeadline.Value <= nowUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredVacancies.Count == 0)
            return;

        var notifications = new List<(List<Guid> Recipients, string Title, int VacancyId)>(expiredVacancies.Count);

        foreach (var vacancy in expiredVacancies)
        {
            vacancy.IsActive = false;
            notifications.Add((
                await db.ClubEmployees
                    .AsNoTracking()
                    .Where(e => e.ClubId == vacancy.ClubId)
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false),
                BuildVacancyTitle(vacancy),
                vacancy.Id));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var notification in notifications)
        {
            foreach (var recipientUserId in notification.Recipients.Where(id => id != Guid.Empty))
            {
                await notificationService.PublishAsync(new NotificationQueueMessage
                {
                    Type = notification.Title,
                    Message = "Closed",
                    Channel = "Push",
                    Source = "club-management",
                    Action = "vacancy.deadline_passed",
                    EntityType = "vacancy",
                    EntityId = notification.VacancyId.ToString(CultureInfo.InvariantCulture),
                    RecipientUserId = recipientUserId
                }).ConfigureAwait(false);
            }
        }
    }

    private void CleanupSentReminderKeys(DateTime nowUtc)
    {
        foreach (var entry in _sentReminderKeys)
        {
            if (nowUtc - entry.Value > SentKeyRetention)
                _sentReminderKeys.TryRemove(entry.Key, out _);
        }
    }

    private static string BuildVacancyTitle(ClubManagementService.Domain.Entities.JobVacancy vacancy)
    {
        if (!string.IsNullOrWhiteSpace(vacancy.ClubPosition?.Title))
            return vacancy.ClubPosition.Title;

        if (!string.IsNullOrWhiteSpace(vacancy.Title))
            return vacancy.Title;

        return $"Vacancy {vacancy.Id.ToString(CultureInfo.InvariantCulture)}";
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string FormatEventDateTime(DateTime eventStartUtc) =>
        NormalizeUtc(eventStartUtc).ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
