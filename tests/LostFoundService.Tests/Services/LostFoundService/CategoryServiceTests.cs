using FluentAssertions;
using global::LostFoundService.Application.DTOs.Category;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Domain.Entities;
using Moq;
using LostFoundService.Tests.Common;

namespace LostFoundService.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock = new();
    private readonly global::LostFoundService.Application.Services.CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new global::LostFoundService.Application.Services.CategoryService(
            _repositoryMock.Object,
            LostFoundMapperFactory.Create());
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldPersistCategory()
    {
        // Arrange
        var request = new CreateCategoryDto { Name = "Electronics" };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>())).Callback<Category>(x => x.Id = 4).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Id.Should().Be(4);
        result.Name.Should().Be("Electronics");
        _repositoryMock.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == request.Name)), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        // Act
        var result = await _service.DeleteAsync(99);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }
}
