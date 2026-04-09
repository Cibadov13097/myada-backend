using FluentAssertions;
using global::LostFoundService.Application.DTOs.FoundItem;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Enums;
using global::LostFoundService.Domain.Exceptions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.LostFoundService;

public class FoundItemAdminServiceTests
{
    private readonly Mock<IFoundItemRepository> _repositoryMock = new();
    private readonly global::LostFoundService.Application.Services.FoundItemAdminService _service;

    public FoundItemAdminServiceTests()
    {
        _service = new global::LostFoundService.Application.Services.FoundItemAdminService(
            _repositoryMock.Object,
            LostFoundMapperFactory.Create());
    }

    [Fact]
    public async Task AdminAddFoundItemAsync_ShouldCreateApprovedItem()
    {
        // Arrange
        var request = new AddFoundItemDto
        {
            Description = "Phone",
            ImageUrl = "phone.png",
            CategoryId = 3
        };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FoundItem>())).Callback<FoundItem>(x => x.Id = 2).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.AdminAddFoundItemAsync(request, "admin-1");

        // Assert
        result.Id.Should().Be(2);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<FoundItem>(f => f.Status == Status.approved && f.CreatedByUserId == "admin-1")), Times.Once);
    }

    [Fact]
    public async Task AdminForceDeleteAsync_WhenItemDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdForAdminAsync(88)).ReturnsAsync((FoundItem)null!);

        // Act
        Func<Task> action = async () => await _service.AdminForceDeleteAsync(88);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Item with id 88 not found.");
    }
}
