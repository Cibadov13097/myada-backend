using System.Linq.Expressions;
using AutoMapper;
using Moq;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Application;

public class CategoryServiceTests
{
    private static void SetupGetAllAsync(Mock<ICategoryRepository> repo, List<Category> data)
    {
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Category, bool>>?>(),
                It.IsAny<Func<IQueryable<Category>, IQueryable<Category>>?>(),
                It.IsAny<Func<IQueryable<Category>, IOrderedQueryable<Category>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(data);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_returns_empty_when_repository_empty()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = TestMapperFactory.Create();
        SetupGetAllAsync(repo, new List<Category>());
        var sut = new CategoryService(repo.Object, mapper);

        var result = await sut.GetAllCategoriesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_returns_mapped_list()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = TestMapperFactory.Create();
        var entities = new List<Category>
        {
            new() { Id = 1, Name = "A", Module = SupportArea.IT },
            new() { Id = 2, Name = "B", Module = SupportArea.FM }
        };
        SetupGetAllAsync(repo, entities);
        var sut = new CategoryService(repo.Object, mapper);

        var result = await sut.GetAllCategoriesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("B", result[1].Name);
    }

    [Fact]
    public async Task GetCategoriesByModuleAsync_uses_repository_and_maps()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = TestMapperFactory.Create();
        repo.Setup(r => r.GetByModuleAsync("IT"))
            .ReturnsAsync(new List<Category> { new() { Id = 1, Name = "Cat", Module = SupportArea.IT } });
        var sut = new CategoryService(repo.Object, mapper);

        var result = await sut.GetCategoriesByModuleAsync("IT");

        Assert.Single(result);
        repo.Verify(r => r.GetByModuleAsync("IT"), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_persists_via_AddAsync()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = TestMapperFactory.Create();
        repo.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => { c.Id = 7; return c; });
        var sut = new CategoryService(repo.Object, mapper);
        var dto = new CategoryCreateDto { Name = "New", Module = SupportArea.IT };

        var result = await sut.CreateCategoryAsync(dto);

        Assert.Equal(7, result.Id);
        repo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "New")), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_removes_when_entity_exists()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = new Mock<IMapper>().Object;
        var entity = new Category { Id = 3, Name = "X", Module = SupportArea.IT };
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(entity);
        repo.Setup(r => r.RemoveAsync(entity)).Returns(Task.CompletedTask);
        var sut = new CategoryService(repo.Object, mapper);

        await sut.DeleteCategoryAsync(3);

        repo.Verify(r => r.RemoveAsync(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_on_category_crud_updates_entity()
    {
        var repo = new Mock<ICategoryRepository>();
        var mapper = TestMapperFactory.Create();
        var entity = new Category { Id = 1, Name = "Old", Module = SupportArea.IT };
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => c);
        var sut = new CategoryService(repo.Object, mapper);
        var crud = (ICategoryCrudService)sut;

        var result = await crud.UpdateAsync(1, new CategoryUpdateDto { Name = "New", Module = SupportArea.FM });

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        repo.Verify(r => r.UpdateAsync(It.Is<Category>(c => c.Name == "New" && c.Module == SupportArea.FM)), Times.Once);
    }
}
