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
using Xunit;

namespace NotificationService.Tests.Layers.Api;

/// <summary>Small service — presentation / API layer guideline ~5–10 tests.</summary>
public sealed class NotificationsControllerLayerTests
{
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
        INotificationService notificationService) =>
        new(db, notificationService, NullLogger<NotificationsController>.Instance);

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
