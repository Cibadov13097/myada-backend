using FluentAssertions;
using global::LocationService.Application.DTOs;
using global::LocationService.Application.Interfaces.Repositories;
using global::LocationService.Application.Services;
using global::LocationService.Domain.Entities;
using Moq;

namespace MyAda.Tests.Services.LocationService;

public class BuildingServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenBuildingsExist_ShouldMapEntitiesToDtos()
    {
        // Arrange
        var buildings = new List<Building>
        {
            new() { Id = 1, Name = "Main Campus" },
            new() { Id = 2, Name = "Library" }
        };

        var repositoryMock = new Mock<IGenericRepository<Building>>();
        repositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(buildings);

        var service = new BuildingService(repositoryMock.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(new[]
        {
            new BuildingDto { Id = 1, Name = "Main Campus" },
            new BuildingDto { Id = 2, Name = "Library" }
        });

        repositoryMock.Verify(repository => repository.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldAddBuildingAndSaveChanges()
    {
        // Arrange
        var request = new CreateBuildingDto
        {
            Name = "Innovation Center"
        };

        var repositoryMock = new Mock<IGenericRepository<Building>>();
        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Building>()))
            .Callback<Building>(building => building.Id = 15)
            .Returns(Task.CompletedTask);

        repositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = new BuildingService(repositoryMock.Object);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Id.Should().Be(15);
        result.Name.Should().Be(request.Name);

        repositoryMock.Verify(
            repository => repository.AddAsync(It.Is<Building>(building => building.Name == request.Name)),
            Times.Once);
        repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenBuildingDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        const int missingId = 99;
        var request = new UpdateBuildingDto
        {
            Name = "Updated Name"
        };

        var repositoryMock = new Mock<IGenericRepository<Building>>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(missingId))
            .ReturnsAsync((Building)null!);

        var service = new BuildingService(repositoryMock.Object);

        // Act
        Func<Task> action = async () => await service.UpdateAsync(missingId, request);

        // Assert
        await action
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Building with id {missingId} not found");

        repositoryMock.Verify(repository => repository.GetByIdAsync(missingId), Times.Once);
        repositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<Building>()), Times.Never);
        repositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }
}
