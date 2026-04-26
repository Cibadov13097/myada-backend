using FluentAssertions;
using global::LostFoundService.Application.DTOs.Category;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Application.Services;
using global::LostFoundService.Domain.Entities;
using Moq;
using LostFoundService.Tests.Common;

namespace LostFoundService.Tests.Layers.Application;

/// <summary>Medium service — application layer guideline ~30–80 tests (extends CategoryService coverage).</summary>
public sealed class LostFoundCategoryApplicationLayerTests
{
    private static CategoryService CreateSut(out Mock<ICategoryRepository> repository)
    {
        repository = new Mock<ICategoryRepository>();
        return new CategoryService(repository.Object, LostFoundMapperFactory.Create());
    }

    [Fact]
    public async Task GetAllAsync_maps_repository_list()
    {
        var sut = CreateSut(out var repo);
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        });

        var result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A");
        result[1].Name.Should().Be("B");
    }

    [Fact]
    public async Task GetByIdAsync_when_found_returns_dto()
    {
        var sut = CreateSut(out var repo);
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Category { Id = 5, Name = "Books" });

        var dto = await sut.GetByIdAsync(5);

        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetByIdAsync_when_missing_returns_null()
    {
        var sut = CreateSut(out var repo);
        repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Category?)null);

        var dto = await sut.GetByIdAsync(9);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_when_found_updates_and_returns_dto()
    {
        var sut = CreateSut(out var repo);
        var entity = new Category { Id = 2, Name = "Old" };
        repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(entity);
        repo.Setup(r => r.UpdateAsync(entity)).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await sut.UpdateAsync(2, new UpdateCategoryDto { Name = "New" });

        dto.Should().NotBeNull();
        dto!.Name.Should().Be("New");
        entity.Name.Should().Be("New");
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_when_missing_returns_null()
    {
        var sut = CreateSut(out var repo);
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync((Category?)null);

        var dto = await sut.UpdateAsync(3, new UpdateCategoryDto { Name = "X" });

        dto.Should().BeNull();
        repo.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("beta")]
    [InlineData("gamma")]
    public async Task CreateAsync_assigns_name_through_mapper(string name)
    {
        var sut = CreateSut(out var repo);
        repo.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .Callback<Category>(c => c.Id = 99)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = await sut.CreateAsync(new CreateCategoryDto { Name = name });

        dto.Name.Should().Be(name);
        dto.Id.Should().Be(99);
    }
}
