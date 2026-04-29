using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence.Seed;

/// <summary>Sample in-app notifications for ADA University demo tenants.</summary>
public static class NotificationDbSeeder
{
    private static readonly Guid DemoUserId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(NotificationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Notifications.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var now = DateTime.UtcNow;

        var items = new[]
        {
            new Notification
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001"),
                Type = "club",
                Message =
                    "ADA Entrepreneurship Club: Startup Weekend registration closes this Friday at 18:00 Baku time.",
                CreatedAt = now.AddDays(-2),
                RecipientUserId = DemoUserId,
                NotificationType = NotificationType.Push
            },
            new Notification
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002"),
                Type = "academic",
                Message =
                    "Spring exam schedule updates are published on the ADA student portal — please review your room assignments.",
                CreatedAt = now.AddDays(-1),
                RecipientUserId = DemoUserId,
                NotificationType = NotificationType.Email
            },
            new Notification
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0003"),
                Type = "campus",
                Message =
                    "Shuttle service to the Shamakhi residential campus: extra departures added for Novruz break week.",
                CreatedAt = now.AddHours(-8),
                RecipientUserId = DemoUserId,
                NotificationType = NotificationType.Push
            },
            new Notification
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0004"),
                Type = "system",
                Message = "MyADA: your profile photo was updated successfully.",
                CreatedAt = now.AddHours(-2),
                RecipientUserId = DemoUserId,
                NotificationType = NotificationType.Push
            }
        };

        await db.Notifications.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
