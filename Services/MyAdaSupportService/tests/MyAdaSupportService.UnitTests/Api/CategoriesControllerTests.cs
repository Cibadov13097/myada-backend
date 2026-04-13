using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaSupportService.Api.Controllers;
using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Api;

public class CategoriesControllerTests
{
    [Fact]
    public async Task GetAll_returns_Ok()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategoryDto>());
        var controller = new CategoriesController(svc.Object);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByModule_returns_Ok()
    {
        var svc = new Mock<ICategoryService>();
        svc.Setup(s => s.GetCategoriesByModuleAsync("IT")).ReturnsAsync(new List<CategoryDto>());
        var controller = new CategoriesController(svc.Object);

        var result = await controller.GetByModule("IT");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_returns_CreatedAtAction()
    {
        var svc = new Mock<ICategoryService>();
        var created = new CategoryDto { Id = 10, Name = "N", Module = SupportArea.IT };
        svc.Setup(s => s.CreateCategoryAsync(It.IsAny<CategoryCreateDto>())).ReturnsAsync(created);
        var controller = new CategoriesController(svc.Object);
        var dto = new CategoryCreateDto { Name = "N", Module = SupportArea.IT };

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

}
