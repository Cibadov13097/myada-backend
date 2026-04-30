using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Persistence;
using System.Net.Mail;

namespace NotificationService.Tests.Controllers;

public class NotificationsControllerTests
{
    [Fact]
    public async Task SendEmail_WhenSmtpRejectsRequest_ShouldReturnBadGatewayProblem()
    {
        await using var dbContext = CreateDbContext();
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(service => service.SendEmailAsync(
                "student@example.com",
                "Test subject",
                "Test body",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException("5.7.0 Authentication Required"));

        var controller = CreateController(dbContext, notificationService.Object);

        var result = await controller.SendEmail(new NotificationsController.SendEmailNotificationRequest
        {
            Type = "manual.test",
            ToEmail = "student@example.com",
            Subject = "Test subject",
            Message = "Test body"
        }, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        details.Title.Should().Be("Email delivery failed");

        (await dbContext.Notifications.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task SendEmail_WhenSmtpConfigurationIsInvalid_ShouldReturnBadGatewayProblem()
    {
        await using var dbContext = CreateDbContext();
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(service => service.SendEmailAsync(
                "student@example.com",
                "Test subject",
                "Test body",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP configuration is missing Smtp:Password."));

        var controller = CreateController(dbContext, notificationService.Object);

        var result = await controller.SendEmail(new NotificationsController.SendEmailNotificationRequest
        {
            Type = "manual.test",
            ToEmail = "student@example.com",
            Subject = "Test subject",
            Message = "Test body"
        }, CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        details.Title.Should().Be("Email delivery is not configured");
        details.Detail.Should().Be("SMTP configuration is missing Smtp:Password.");

        (await dbContext.Notifications.AnyAsync()).Should().BeFalse();
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
        NotificationDbContext dbContext,
        INotificationService notificationService)
    {
        var realtimeNotifier = new Mock<INotificationRealtimeNotifier>();
        return new NotificationsController(
            dbContext,
            notificationService,
            realtimeNotifier.Object,
            NullLogger<NotificationsController>.Instance);
    }
}
