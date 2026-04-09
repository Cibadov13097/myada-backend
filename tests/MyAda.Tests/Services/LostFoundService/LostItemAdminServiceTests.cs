using FluentAssertions;
using global::LostFoundService.Application.DTOs.LostItem;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Exceptions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.LostFoundService;

public class LostItemAdminServiceTests
{
    private readonly Mock<ILostItemRepository> _repositoryMock = new();
    private readonly global::LostFoundService.Application.Services.LostItemAdminService _service;

    public LostItemAdminServiceTests()
    {
        _service = new global::LostFoundService.Application.Services.LostItemAdminService(
            _repositoryMock.Object,
            LostFoundMapperFactory.Create());
    }

    [Fact]
    public async Task AdminAddLostItemAsync_ShouldCreateItem()
    {
        // Arrange
        var request = new AddLostItemDto
        {
            Title = "Laptop",
            Description = "Gray",
            CategoryId = 2,
            Image = "laptop.png"
        };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<LostItem>())).Callback<LostItem>(x => x.Id = 12).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.AdminAddLostItemAsync(request, "admin-1");

        // Assert
        result.Id.Should().Be(12);
        result.PostedByUserId.Should().Be("admin-1");
    }

    [Fact]
    public async Task AdminDeleteAsync_WhenItemDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdForAdminAsync(77)).ReturnsAsync((LostItem?)null);

        // Act
        Func<Task> action = async () => await _service.AdminDeleteAsync(77, "admin-1");

        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("LostItem with id 77 not found");
    }
}
