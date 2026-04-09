using FluentAssertions;
using LocationService.Api.Controllers;
using LocationService.Application.DTOs;
using LocationService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MyAda.Tests.Controllers.LocationService;

public class BuildingsControllerTests
{
    [Fact]
    public async Task GetById_WhenBuildingExists_ShouldReturnOkResult()
    {
        // Arrange
        var building = new BuildingDto
        {
            Id = 7,
            Name = "Engineering"
        };

        var serviceMock = new Mock<IBuildingService>();
        serviceMock
            .Setup(service => service.GetByIdAsync(building.Id))
            .ReturnsAsync(building);

        var controller = new BuildingsController(serviceMock.Object);

        // Act
        var result = await controller.GetById(building.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(building);

        serviceMock.Verify(service => service.GetByIdAsync(building.Id), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenBuildingDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        const int missingId = 404;

        var serviceMock = new Mock<IBuildingService>();
        serviceMock
            .Setup(service => service.GetByIdAsync(missingId))
            .ReturnsAsync((BuildingDto?)null);

        var controller = new BuildingsController(serviceMock.Object);

        // Act
        var result = await controller.GetById(missingId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        serviceMock.Verify(service => service.GetByIdAsync(missingId), Times.Once);
    }
}
