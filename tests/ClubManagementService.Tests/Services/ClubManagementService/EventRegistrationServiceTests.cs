using global::ClubManagementService.Application.DTOs.EventRegistration;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Application.Interfaces.Services;
using global::ClubManagementService.Domain.Entities;
using global::ClubManagementService.Infrastructure.Persistence.Context;
using global::ClubManagementService.Infrastructure.Services;
using FluentAssertions;
using ClubManagementService.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Contracts.Events.Notifications;

namespace ClubManagementService.Tests.Services;

public class EventRegistrationServiceTests : IDisposable
{
    private readonly Mock<IEventRegistrationRepository> _repositoryMock = new();
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IClubNotificationService> _notificationServiceMock = new();
    private readonly Mock<ITicketJwtService> _ticketJwtServiceMock = new();
    private readonly ClubManagementDbContext _dbContext;
    private readonly EventRegistrationService _service;

    public EventRegistrationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _dbContext = new ClubManagementDbContext(dbOptions);
        _notificationServiceMock
            .Setup(x => x.PublishAsync(It.IsAny<NotificationQueueMessage>()))
            .Returns(Task.CompletedTask);

        _service = new EventRegistrationService(
            _repositoryMock.Object,
            _eventRepositoryMock.Object,
            ClubManagementMapperFactory.Create(),
            _notificationServiceMock.Object,
            _ticketJwtServiceMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnEventRegistrations()
    {
        const int eventId = 42;
        _repositoryMock.Setup(x => x.GetByEventIdAsync(eventId)).ReturnsAsync(new List<EventRegistration>
        {
            new() { Id = 1, EventId = eventId, UserId = Guid.NewGuid() }
        });

        var result = await _service.GetByEventIdAsync(eventId);

        result.Should().ContainSingle();
        result[0].EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task DeleteAsync_WhenRegistrationDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(15)).ReturnsAsync((EventRegistration?)null);

        Func<Task> action = async () => await _service.DeleteAsync(15);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("EventRegistration 15 not found.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldPublishNotificationForRegisteredUser()
    {
        var userId = Guid.NewGuid();
        const int eventId = 42;
        var evt = new Event
        {
            Id = eventId,
            Name = "AI Career Fair",
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            Location = "Main Hall",
            SeatLimit = 10
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId)).ReturnsAsync(evt);
        _repositoryMock.Setup(x => x.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventRegistration?)null);
        _repositoryMock.Setup(x => x.GetByEventIdAsync(eventId)).ReturnsAsync(new List<EventRegistration>());
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<EventRegistration>()))
            .Callback<EventRegistration>(registration => registration.Id = 100)
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _ticketJwtServiceMock.Setup(x => x.CreateTicketJwt(
                userId,
                eventId,
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()))
            .Returns("ticket-jwt");

        var result = await _service.RegisterAsync(eventId, userId);

        result.EventId.Should().Be(eventId);
        result.UserId.Should().Be(userId);
        _notificationServiceMock.Verify(
            x => x.PublishAsync(It.Is<NotificationQueueMessage>(message =>
                message.Action == "event.registration.created"
                && message.ActorUserId == userId
                && message.RecipientUserId == userId
                && message.Channel == "Push")),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishRegistrationNotificationOnlyOnce()
    {
        var userId = Guid.NewGuid();
        const int eventId = 77;
        var evt = new Event
        {
            Id = eventId,
            Name = "Hackathon",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(3),
            Location = "Innovation Lab",
            SeatLimit = 25
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId)).ReturnsAsync(evt);
        _repositoryMock.Setup(x => x.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventRegistration?)null);
        _repositoryMock.Setup(x => x.GetByEventIdAsync(eventId)).ReturnsAsync(new List<EventRegistration>());
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<EventRegistration>()))
            .Callback<EventRegistration>(registration => registration.Id = 200)
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _ticketJwtServiceMock.Setup(x => x.CreateTicketJwt(
                userId,
                eventId,
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()))
            .Returns("ticket-jwt");

        var result = await _service.CreateAsync(new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId
        });

        result.EventId.Should().Be(eventId);
        result.UserId.Should().Be(userId);
        _notificationServiceMock.Verify(
            x => x.PublishAsync(It.Is<NotificationQueueMessage>(message =>
                message.Action == "event.registration.created"
                && message.RecipientUserId == userId)),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAttendanceTokenThatExpiresInFiveSeconds()
    {
        var userId = Guid.NewGuid();
        const int eventId = 88;
        DateTimeOffset capturedNow = default;
        DateTimeOffset capturedExpiresAt = default;
        var evt = new Event
        {
            Id = eventId,
            Name = "Quick Check-In Event",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Location = "Hall A",
            SeatLimit = 30
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId)).ReturnsAsync(evt);
        _repositoryMock.Setup(x => x.GetByEventAndUserAsync(eventId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventRegistration?)null);
        _repositoryMock.Setup(x => x.GetByEventIdAsync(eventId)).ReturnsAsync(new List<EventRegistration>());
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<EventRegistration>()))
            .Callback<EventRegistration>(registration => registration.Id = 300)
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        _ticketJwtServiceMock.Setup(x => x.CreateTicketJwt(
                userId,
                eventId,
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()))
            .Callback<Guid, int, string, DateTimeOffset, DateTimeOffset>((_, _, _, now, expiresAt) =>
            {
                capturedNow = now;
                capturedExpiresAt = expiresAt;
            })
            .Returns("ticket-jwt");

        var result = await _service.RegisterAsync(eventId, userId);

        result.EventId.Should().Be(eventId);
        (capturedExpiresAt - capturedNow).Should().BeCloseTo(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        _ticketJwtServiceMock.Verify(x => x.CreateTicketJwt(
                userId,
                eventId,
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
