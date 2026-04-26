using FluentAssertions;
using global::LostFoundService.Api.Controllers;
using global::LostFoundService.Application.DTOs.Category;
using global::LostFoundService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LostFoundService.Tests.Layers.Api;

/// <summary>Medium service — presentation / API layer guideline ~10–30 tests.</summary>
public sealed class LostFoundCategoryApiLayerTests
{
    [Fact]
    public async Task GetAll_returns_ok_with_payload()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CategoryDto>());
        var controller = new CategoryController(service.Object);

        var result = await controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<CategoryDto>>();
    }

    [Fact]
    public async Task GetById_returns_not_found_when_service_returns_null()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((CategoryDto?)null);
        var controller = new CategoryController(service.Object);

        var result = await controller.GetById(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_returns_ok_when_found()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(new CategoryDto { Id = 2, Name = "X" });
        var controller = new CategoryController(service.Object);

        var result = await controller.GetById(2);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<CategoryDto>().Subject;
        dto.Name.Should().Be("X");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_returns_bad_request_when_name_missing(string name)
    {
        var service = new Mock<ICategoryService>();
        var controller = new CategoryController(service.Object);

        var result = await controller.Create(new CreateCategoryDto { Name = name });

        result.Should().BeOfType<BadRequestObjectResult>();
        service.Verify(s => s.CreateAsync(It.IsAny<CreateCategoryDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_returns_created_when_valid()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateCategoryDto>()))
            .ReturnsAsync(new CategoryDto { Id = 10, Name = "New" });
        var controller = new CategoryController(service.Object);

        var result = await controller.Create(new CreateCategoryDto { Name = "New" });

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_returns_bad_request_when_name_missing(string name)
    {
        var service = new Mock<ICategoryService>();
        var controller = new CategoryController(service.Object);

        var result = await controller.Update(1, new UpdateCategoryDto { Name = name });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_returns_not_found_when_service_returns_null()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateCategoryDto>())).ReturnsAsync((CategoryDto?)null);
        var controller = new CategoryController(service.Object);

        var result = await controller.Update(1, new UpdateCategoryDto { Name = "Ok" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_returns_no_content_when_deleted()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.DeleteAsync(3)).ReturnsAsync(true);
        var controller = new CategoryController(service.Object);

        var result = await controller.Delete(3);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_returns_not_found_when_false()
    {
        var service = new Mock<ICategoryService>();
        service.Setup(s => s.DeleteAsync(4)).ReturnsAsync(false);
        var controller = new CategoryController(service.Object);

        var result = await controller.Delete(4);

        result.Should().BeOfType<NotFoundResult>();
    }
}
