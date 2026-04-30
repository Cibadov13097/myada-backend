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
    public async Task CreateAsync_ShouldNormalizeUnspecifiedTimesToUtcBeforePersisting()
    {
        var request = new CreateEventDto
        {
            ClubId = 1,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = new DateTime(2026, 4, 2, 20, 44, 0, DateTimeKind.Unspecified),
            EndTime = new DateTime(2026, 4, 2, 23, 44, 0, DateTimeKind.Unspecified),
            Location = "Hall A",
            SeatLimit = 100,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid()
        };
        Event? addedEntity = null;
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Event>()))
            .Callback<Event>(x =>
            {
                addedEntity = x;
                x.Id = 11;
            })
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        request.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        request.EndTime.Kind.Should().Be(DateTimeKind.Utc);
        addedEntity.Should().NotBeNull();
        addedEntity!.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        addedEntity.EndTime.Kind.Should().Be(DateTimeKind.Utc);
        result.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        result.EndTime.Kind.Should().Be(DateTimeKind.Utc);
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
    public async Task CreateAsync_WhenEndTimeIsNotAfterStartTime_ShouldThrowArgumentException()
    {
        var start = DateTime.UtcNow;
        var request = new CreateEventDto
        {
            ClubId = 1,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = start,
            EndTime = start,
            Location = "Hall A",
            SeatLimit = 100,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid()
        };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("endTime must be after startTime.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateAsync_WhenSeatLimitIsNotPositive_ShouldThrowArgumentException(int seatLimit)
    {
        var request = new CreateEventDto
        {
            ClubId = 1,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(4),
            Location = "Hall A",
            SeatLimit = seatLimit,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid()
        };

        Func<Task> action = async () => await _service.CreateAsync(request);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("seatLimit must be greater than 0.");
    }

    [Fact]
    public async Task UpdateAsync_WhenEventDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(22)).ReturnsAsync((Event?)null);

        Func<Task> action = async () => await _service.UpdateAsync(22, new UpdateEventDto());

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Event 22 not found.");
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusIsInvalid_ShouldThrowArgumentException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(22)).ReturnsAsync(new Event
        {
            Id = 22,
            ClubId = 1,
            Name = "Existing event",
            Description = "Existing description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Location = "Hall A",
            SeatLimit = 80,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid(),
            Status = ClubStatus.Pending
        });

        var dto = new UpdateEventDto
        {
            Name = "Hackathon",
            Description = "Updated description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            Location = "Hall A",
            SeatLimit = 80,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            Status = (ClubStatus)999
        };

        Func<Task> action = async () => await _service.UpdateAsync(22, dto);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("status is invalid.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldNormalizeUnspecifiedTimesToUtcBeforePersisting()
    {
        var existing = new Event
        {
            Id = 22,
            ClubId = 1,
            Name = "Existing event",
            Description = "Existing description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Location = "Hall A",
            SeatLimit = 80,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = Guid.NewGuid(),
            Status = ClubStatus.Pending
        };
        _repositoryMock.Setup(x => x.GetByIdAsync(22)).ReturnsAsync(existing);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Event>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new UpdateEventDto
        {
            Name = "Updated event",
            Description = "Updated description",
            StartTime = new DateTime(2026, 4, 2, 20, 44, 0, DateTimeKind.Unspecified),
            EndTime = new DateTime(2026, 4, 2, 23, 44, 0, DateTimeKind.Unspecified),
            Location = "Hall B",
            SeatLimit = 120,
            Requirements = "Projector",
            Prerequisites = "Registration",
            Notes = "Bring ID",
            Status = ClubStatus.Approved
        };

        var result = await _service.UpdateAsync(22, dto);

        dto.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        dto.EndTime.Kind.Should().Be(DateTimeKind.Utc);
        existing.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        existing.EndTime.Kind.Should().Be(DateTimeKind.Utc);
        result.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        result.EndTime.Kind.Should().Be(DateTimeKind.Utc);
    }
}
