using FluentAssertions;
using global::LostFoundService.Application.DTOs.FoundItem;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Application.Interfaces.Services;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Exceptions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.LostFoundService;

public class FoundItemServiceTests
{
    private readonly Mock<IFoundItemRepository> _repositoryMock = new();
    private readonly Mock<ILocationServiceClient> _locationClientMock = new();
    private readonly global::LostFoundService.Application.Services.FoundItemService _service;

    public FoundItemServiceTests()
    {
        _service = new global::LostFoundService.Application.Services.FoundItemService(
            _repositoryMock.Object,
            LostFoundMapperFactory.Create(),
            _locationClientMock.Object);
    }

    [Fact]
    public async Task AddFoundItemAsync_WhenLocationIsValid_ShouldCreateItem()
    {
        // Arrange
        var request = new AddFoundItemDto
        {
            Description = "Wallet",
            ImageUrl = "wallet.png",
            BuildingId = 1,
            RoomId = 10,
            CategoryId = 2
        };

        _locationClientMock
            .Setup(x => x.LocationExistsAsync(request.BuildingId, request.RoomId))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<FoundItem>()))
            .Callback<FoundItem>(x => x.Id = 5)
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddFoundItemAsync(request, "user-1");

        // Assert
        result.Id.Should().Be(5);
        result.FoundByUserId.Should().Be("user-1");
        result.BuildingId.Should().Be(1);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<FoundItem>(f => f.FoundByUserId == "user-1")), Times.Once);
    }

    [Fact]
    public async Task AddFoundItemAsync_WhenOnlyOneLocationValueIsProvided_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new AddFoundItemDto
        {
            Description = "Wallet",
            BuildingId = 1,
            CategoryId = 2
        };

        // Act
        Func<Task> action = async () => await _service.AddFoundItemAsync(request, "user-1");

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Location requires both BuildingId and RoomId when provided.");
    }

    [Fact]
    public async Task UpdateFoundItemAsync_WhenLocationServiceFails_ShouldThrowServiceUnavailableException()
    {
        // Arrange
        var entity = new FoundItem { Id = 7, Description = "Old", FoundByUserId = "user-1", CreatedByUserId = "user-1", CategoryId = 1 };
        var request = new UpdateFoundItemDto
        {
            Description = "Updated",
            BuildingId = 2,
            RoomId = 20
        };

        _repositoryMock.Setup(x => x.GetByIdForUserAsync(7, "user-1")).ReturnsAsync(entity);
        _locationClientMock.Setup(x => x.LocationExistsAsync(2, 20)).ThrowsAsync(new Exception("down"));

        // Act
        Func<Task> action = async () => await _service.UpdateFoundItemAsync(7, request, "user-1");

        // Assert
        await action.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("Location service is unavailable");
    }

    [Fact]
    public async Task DeleteFoundItemAsync_WhenItemExists_ShouldSoftDeleteItem()
    {
        // Arrange
        var entity = new FoundItem { Id = 3, FoundByUserId = "user-1", CreatedByUserId = "user-1", CategoryId = 1 };
        _repositoryMock.Setup(x => x.GetByIdForUserAsync(3, "user-1")).ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity)).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFoundItemAsync(3, "user-1");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedByUserId.Should().Be("user-1");
        _repositoryMock.Verify(x => x.UpdateAsync(entity), Times.Once);
    }
}
