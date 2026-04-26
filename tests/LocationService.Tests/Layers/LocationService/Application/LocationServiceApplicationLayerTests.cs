using FluentAssertions;
using global::LocationService.Application.DTOs;
using global::LocationService.Application.Interfaces.Repositories;
using global::LocationService.Application.Services;
using global::LocationService.Domain.Entities;
using Moq;

namespace LocationService.Tests.Layers.Application;

/// <summary>Medium service — application layer guideline ~30–80 tests (complements existing BuildingServiceTests).</summary>
public sealed class LocationServiceApplicationLayerTests
{
    [Fact]
    public async Task GetByIdAsync_when_found_returns_dto()
    {
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Building { Id = 3, Name = "Lab" });
        var service = new BuildingService(repo.Object);

        var dto = await service.GetByIdAsync(3);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(3);
        dto.Name.Should().Be("Lab");
    }

    [Fact]
    public async Task GetByIdAsync_when_missing_returns_null()
    {
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Building?)null);
        var service = new BuildingService(repo.Object);

        var dto = await service.GetByIdAsync(404);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_when_empty_returns_empty_list()
    {
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Building>());
        var service = new BuildingService(repo.Object);

        var list = await service.GetAllAsync();

        list.Should().BeEmpty();
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Building with unicode — 中文")]
    [InlineData("  trimmed-on-entity  ")]
    public async Task CreateAsync_persists_name(string name)
    {
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.AddAsync(It.IsAny<Building>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var service = new BuildingService(repo.Object);

        var dto = await service.CreateAsync(new CreateBuildingDto { Name = name });

        dto.Name.Should().Be(name);
        repo.Verify(r => r.AddAsync(It.Is<Building>(b => b.Name == name)), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_when_found_invokes_repository_delete()
    {
        var entity = new Building { Id = 7, Name = "X" };
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(entity);
        repo.Setup(r => r.DeleteAsync(entity)).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var service = new BuildingService(repo.Object);

        await service.DeleteAsync(7);

        repo.Verify(r => r.DeleteAsync(entity), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_when_missing_throws()
    {
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Building?)null);
        var service = new BuildingService(repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(1));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(10, 200)]
    [InlineData(50, 50)]
    public async Task GetAllAsync_preserves_order_from_repository(int firstId, int secondId)
    {
        var list = new List<Building>
        {
            new() { Id = firstId, Name = "a" },
            new() { Id = secondId, Name = "b" }
        };
        var repo = new Mock<IGenericRepository<Building>>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
        var service = new BuildingService(repo.Object);

        var result = await service.GetAllAsync();

        result.Select(r => r.Id).Should().Equal(firstId, secondId);
    }
}
