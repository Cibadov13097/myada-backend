using global::ClubManagementService.Application.DTOs.Category;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Domain.Entities;
using FluentAssertions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.ClubManagementService;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.CategoryService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create());
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldPersistCategory()
    {
        var request = new CreateCategoryDto { Name = "Technology" };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>())).Callback<Category>(x => x.Id = 5).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Id.Should().Be(5);
        result.Name.Should().Be("Technology");
        _repositoryMock.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == request.Name)), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        Func<Task> action = async () => await _service.UpdateAsync(99, new UpdateCategoryDto { Name = "Updated" });

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Category 99 not found.");
    }
}
