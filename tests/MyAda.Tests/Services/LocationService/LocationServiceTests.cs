using FluentAssertions;
using global::LocationService.Application.DTOs;
using global::LocationService.Application.Interfaces.Repositories;
using global::LocationService.Domain.Entities;
using Moq;

namespace MyAda.Tests.Services.LocationService;

public class LocationServiceTests
{
    [Fact]
    public async Task ValidateRoomLocationAsync_WhenRoomBelongsToBuilding_ShouldReturnTrue()
    {
        // Arrange
        var request = new ValidateRoomLocationDto
        {
            RoomId = 12,
            BuildingId = 3
        };

        var roomRepositoryMock = new Mock<IGenericRepository<Room>>();
        roomRepositoryMock
            .Setup(repository => repository.GetByIdAsync(request.RoomId))
            .ReturnsAsync(new Room
            {
                Id = request.RoomId,
                BuildingId = request.BuildingId,
                Name = "Lab 12"
            });

        var buildingRepositoryMock = new Mock<IGenericRepository<Building>>();
        buildingRepositoryMock
            .Setup(repository => repository.GetByIdAsync(request.BuildingId))
            .ReturnsAsync(new Building
            {
                Id = request.BuildingId,
                Name = "Science Block"
            });

        var service = new global::LocationService.Application.Services.LocationService(
            roomRepositoryMock.Object,
            buildingRepositoryMock.Object);

        // Act
        var result = await service.ValidateRoomLocationAsync(request);

        // Assert
        result.Should().BeTrue();

        roomRepositoryMock.Verify(repository => repository.GetByIdAsync(request.RoomId), Times.Once);
        buildingRepositoryMock.Verify(repository => repository.GetByIdAsync(request.BuildingId), Times.Once);
    }

    [Fact]
    public async Task ValidateRoomLocationAsync_WhenBuildingDoesNotMatchRoom_ShouldReturnFalse()
    {
        // Arrange
        var request = new ValidateRoomLocationDto
        {
            RoomId = 8,
            BuildingId = 2
        };

        var roomRepositoryMock = new Mock<IGenericRepository<Room>>();
        roomRepositoryMock
            .Setup(repository => repository.GetByIdAsync(request.RoomId))
            .ReturnsAsync(new Room
            {
                Id = request.RoomId,
                BuildingId = 10,
                Name = "Studio"
            });

        var buildingRepositoryMock = new Mock<IGenericRepository<Building>>();
        buildingRepositoryMock
            .Setup(repository => repository.GetByIdAsync(request.BuildingId))
            .ReturnsAsync(new Building
            {
                Id = request.BuildingId,
                Name = "Arts Center"
            });

        var service = new global::LocationService.Application.Services.LocationService(
            roomRepositoryMock.Object,
            buildingRepositoryMock.Object);

        // Act
        var result = await service.ValidateRoomLocationAsync(request);

        // Assert
        result.Should().BeFalse();

        roomRepositoryMock.Verify(repository => repository.GetByIdAsync(request.RoomId), Times.Once);
        buildingRepositoryMock.Verify(repository => repository.GetByIdAsync(request.BuildingId), Times.Once);
    }
}
