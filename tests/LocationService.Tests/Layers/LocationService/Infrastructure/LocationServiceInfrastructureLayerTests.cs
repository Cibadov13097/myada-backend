using FluentAssertions;
using LocationService.Domain.Entities;
using LocationService.Infrastructure.Persistence.Context;
using LocationService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocationService.Tests.Layers.Infrastructure;

/// <summary>Medium service — infrastructure layer guideline ~5–20 tests.</summary>
public sealed class LocationServiceInfrastructureLayerTests
{
    private static LocationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new LocationDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task GenericRepository_Add_and_GetById_roundtrip()
    {
        await using var ctx = CreateContext();
        var repo = new GenericRepository<Building>(ctx);
        var b = new Building { Name = "Science" };
        await repo.AddAsync(b);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(b.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Science");
    }

    [Fact]
    public async Task GenericRepository_GetAll_returns_tracked_entities()
    {
        await using var ctx = CreateContext();
        var repo = new GenericRepository<Building>(ctx);
        await repo.AddAsync(new Building { Name = "A" });
        await repo.AddAsync(new Building { Name = "B" });
        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();

        all.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GenericRepository_Update_persists_changes()
    {
        await using var ctx = CreateContext();
        var repo = new GenericRepository<Building>(ctx);
        var b = new Building { Name = "Old" };
        await repo.AddAsync(b);
        await repo.SaveChangesAsync();

        b.Name = "New";
        await repo.UpdateAsync(b);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(b.Id);
        loaded!.Name.Should().Be("New");
    }
}
