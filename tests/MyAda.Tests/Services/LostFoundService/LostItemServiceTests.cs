using FluentAssertions;
using global::LostFoundService.Application.DTOs.LostItem;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Application.Interfaces.Services;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyAda.Tests.Common;
using Shared.Shared.Infrastructure;

namespace MyAda.Tests.Services.LostFoundService;

public class LostItemServiceTests
{
    private readonly Mock<ILostItemRepository> _repositoryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILocationServiceClient> _locationClientMock = new();
    private readonly global::LostFoundService.Application.Services.LostItemService _service;

    public LostItemServiceTests()
    {
        _service = new global::LostFoundService.Application.Services.LostItemService(
            _repositoryMock.Object,
            LostFoundMapperFactory.Create(),
            _publisherMock.Object,
            _locationClientMock.Object,
            NullLogger<global::LostFoundService.Application.Services.LostItemService>.Instance);
    }

    [Fact]
    public async Task AddLostItemAsync_WhenUserIsMissing_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new AddLostItemDto { Title = "Bag", Description = "Black", CategoryId = 1, Image = "bag.png" };

        // Act
        Func<Task> action = async () => await _service.AddLostItemAsync(request, "");

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User is not authenticated");
    }

    [Fact]
    public async Task AddLostItemAsync_WhenLocationIsValid_ShouldCreateItem()
    {
        // Arrange
        var request = new AddLostItemDto
        {
            Title = "Bag",
            Description = "Black",
            CategoryId = 1,
            Image = "bag.png",
            BuildingId = 1,
            RoomId = 11
        };

        _locationClientMock.Setup(x => x.LocationExistsAsync(1, 11)).ReturnsAsync(true);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<LostItem>())).Callback<LostItem>(x => x.Id = 6).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddLostItemAsync(request, "user-5");

        // Assert
        result.Id.Should().Be(6);
        result.PostedByUserId.Should().Be("user-5");
        _repositoryMock.Verify(x => x.AddAsync(It.Is<LostItem>(l => l.PostedByUserId == "user-5")), Times.Once);
    }

    [Fact]
    public async Task UpdateLostItemAsync_WhenLocationDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var entity = new LostItem { Id = 2, Title = "Bag", Description = "Old", PostedByUserId = "user-5", CategoryId = 1, ImageUrl = "old.png" };
        var request = new UpdateLostItemDto
        {
            Title = "Bag",
            Description = "New",
            Image = "new.png",
            BuildingId = 3,
            RoomId = 30
        };

        _repositoryMock.Setup(x => x.GetByIdForUserAsync(2, "user-5")).ReturnsAsync(entity);
        _locationClientMock.Setup(x => x.LocationExistsAsync(3, 30)).ReturnsAsync(false);

        // Act
        Func<Task> action = async () => await _service.UpdateLostItemAsync(2, request, "user-5");

        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Building or Room not found.");
    }

    [Fact]
    public async Task DeleteLostItemAsync_WhenItemExists_ShouldSoftDeleteItem()
    {
        // Arrange
        var entity = new LostItem { Id = 9, Title = "Keys", Description = "Silver", PostedByUserId = "user-5", CategoryId = 1, ImageUrl = "keys.png" };
        _repositoryMock.Setup(x => x.GetByIdForUserAsync(9, "user-5")).ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity)).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteLostItemAsync(9, "user-5");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedByUserId.Should().Be("user-5");
        _repositoryMock.Verify(x => x.UpdateAsync(entity), Times.Once);
    }
}
