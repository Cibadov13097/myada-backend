using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using Xunit;

namespace NotificationService.Tests.Layers.Infrastructure;

/// <summary>Small service — infrastructure layer guideline ~0–5 unit tests.</summary>
public sealed class NotificationInfrastructureLayerTests
{
    private static NotificationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new NotificationDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task NotificationDbContext_persists_notification()
    {
        await using var ctx = CreateContext();
        var entity = new Notification
        {
            Id = Guid.NewGuid(),
            Type = "t",
            Message = "m",
            CreatedAt = DateTime.UtcNow,
            NotificationType = NotificationType.Push
        };
        ctx.Notifications.Add(entity);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Notifications.AsNoTracking().SingleAsync(n => n.Id == entity.Id);
        loaded.NotificationType.Should().Be(NotificationType.Push);
    }

    [Fact]
    public void SmtpOptions_defaults()
    {
        var o = new global::NotificationService.Infrastructure.Email.SmtpOptions();
        o.Port.Should().Be(587);
        o.EnableSsl.Should().BeTrue();
        o.FromName.Should().Be("MyADA Notifications");
    }
}
