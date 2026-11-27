using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;
using System.Security.Claims;
using Xunit;

namespace NotificationService.Tests.Layers.Api;

/// <summary>Small service — presentation / API layer guideline ~5–10 tests.</summary>
public sealed class NotificationsControllerLayerTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class ConcurrencyFailingNotificationDbContext : NotificationDbContext
    {
        public ConcurrencyFailingNotificationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public bool ThrowOnSaveChanges { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnSaveChanges)
            {
                throw new DbUpdateConcurrencyException("Simulated concurrent delete.");
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private static NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new NotificationDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private static NotificationsController CreateController(
        NotificationDbContext db,
        INotificationService notificationService)
    {
        var realtimeNotifier = new Mock<INotificationRealtimeNotifier>();
        var controller = new NotificationsController(
            db,
            notificationService,
            realtimeNotifier.Object,
            NullLogger<NotificationsController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim("sub", CurrentUserId.ToString())
                    ],
                    authenticationType: "TestAuth"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetAll_returns_empty_page()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_only_returns_notifications_for_current_user()
    {
        await using var db = CreateDbContext();
        db.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                Type = "mine",
                Message = "visible",
                CreatedAt = DateTime.UtcNow,
                RecipientUserId = CurrentUserId,
                NotificationType = NotificationType.Push
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                Type = "other",
                Message = "hidden",
                CreatedAt = DateTime.UtcNow,
                RecipientUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                NotificationType = NotificationType.Push
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                Type = "legacy-null",
                Message = "hidden",
                CreatedAt = DateTime.UtcNow,
                RecipientUserId = null,
                NotificationType = NotificationType.Push
            });
        await db.SaveChangesAsync();

        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value!.GetType().GetProperty("items")!.GetValue(ok.Value) as System.Collections.IEnumerable;
        items.Should().NotBeNull();
        items!.Cast<object>().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAll_invalid_channel_returns_bad_request()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.GetAll(channel: "not-a-channel");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_returns_not_found()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_returns_not_found_when_missing()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_removes_existing_row()
    {
        await using var db = CreateDbContext();
        var id = Guid.NewGuid();
        db.Notifications.Add(new Notification
        {
            Id = id,
            Type = "t",
            Message = "m",
            CreatedAt = DateTime.UtcNow,
            RecipientUserId = CurrentUserId,
            NotificationType = NotificationType.Email
        });
        await db.SaveChangesAsync();

        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        (await db.Notifications.AnyAsync(n => n.Id == id)).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_returns_no_content_when_row_was_deleted_concurrently()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ConcurrencyFailingNotificationDbContext(options);
        db.Database.EnsureCreated();

        var id = Guid.NewGuid();
        db.Notifications.Add(new Notification
        {
            Id = id,
            Type = "t",
            Message = "m",
            CreatedAt = DateTime.UtcNow,
            RecipientUserId = CurrentUserId,
            NotificationType = NotificationType.Email
        });
        await db.SaveChangesAsync();

        db.ThrowOnSaveChanges = true;

        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SendEmail_success_persists_row()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        mock.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(db, mock.Object);

        var result = await controller.SendEmail(new NotificationsController.SendEmailNotificationRequest
        {
            ToEmail = "user@example.com",
            Subject = "Hello",
            Message = "Body"
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        (await db.Notifications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SendEmail_with_current_user_broadcasts_realtime_notification()
    {
        await using var db = CreateDbContext();
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var realtimeNotifier = new Mock<INotificationRealtimeNotifier>();
        var controller = new NotificationsController(
            db,
            notificationService.Object,
            realtimeNotifier.Object,
            NullLogger<NotificationsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                        [
                            new Claim("sub", CurrentUserId.ToString())
                        ],
                        authenticationType: "TestAuth"))
                }
            }
        };

        await controller.SendEmail(new NotificationsController.SendEmailNotificationRequest
        {
            ToEmail = "user@example.com",
            Subject = "Hello",
            Message = "Body"
        }, CancellationToken.None);

        realtimeNotifier.Verify(
            notifier => notifier.NotifyNotificationCreatedAsync(
                It.Is<Notification>(n => n.RecipientUserId == CurrentUserId && n.NotificationType == NotificationType.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmail_rejects_whitespace_only_fields()
    {
        await using var db = CreateDbContext();
        var mock = new Mock<INotificationService>();
        var controller = CreateController(db, mock.Object);

        var result = await controller.SendEmail(new NotificationsController.SendEmailNotificationRequest
        {
            ToEmail = "   ",
            Subject = "   ",
            Message = "   "
        }, CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
