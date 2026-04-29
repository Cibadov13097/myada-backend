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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
