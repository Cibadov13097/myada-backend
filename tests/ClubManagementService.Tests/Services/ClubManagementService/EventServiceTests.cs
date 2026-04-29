using global::ClubManagementService.Application.DTOs.Event;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Application.Interfaces.Services;
using global::ClubManagementService.Domain.Entities;
using global::ClubManagementService.Domain.Enums;
using FluentAssertions;
using Moq;
using ClubManagementService.Tests.Common;

namespace ClubManagementService.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repositoryMock = new();
    private readonly Mock<IClubNotificationService> _notificationServiceMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.EventService _service;

    public EventServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.EventService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create(),
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetPendingStatus()
    {
        var request = new CreateEventDto
        {
            ClubId = 1,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(4),
            Location = "Hall A",
            BuildingId = 2,
            RoomId = 204,
            SeatLimit = 100,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid()
        };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Event>())).Callback<Event>(x => x.Id = 11).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Id.Should().Be(11);
        result.Status.Should().Be(ClubStatus.Pending);
    }

    [Fact]
    public async Task CreateAsync_WhenOnlyOneLocationIdIsProvided_ShouldThrowArgumentException()
    {
        var request = new CreateEventDto
        {
            ClubId = 1,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(4),
            Location = "Hall A",
            BuildingId = 2,
            SeatLimit = 100,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid()
        };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("buildingId and roomId must be provided together.");
    }

    [Fact]
    public async Task UpdateAsync_WhenEventDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(22)).ReturnsAsync((Event?)null);

        Func<Task> action = async () => await _service.UpdateAsync(22, new UpdateEventDto());

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Event 22 not found.");
    }
}
