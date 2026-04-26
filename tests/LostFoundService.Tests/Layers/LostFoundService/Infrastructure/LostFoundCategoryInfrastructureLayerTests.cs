using FluentAssertions;
using LostFoundService.Domain.Entities;
using LostFoundService.Infrastructure.Persistence.Context;
using LostFoundService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Tests.Layers.Infrastructure;

/// <summary>Medium service — infrastructure layer guideline ~5–20 tests.</summary>
public sealed class LostFoundCategoryInfrastructureLayerTests
{
    private static LostFoundDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LostFoundDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new LostFoundDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task CategoryRepository_GetAllAsync_orders_by_name()
    {
        await using var ctx = CreateContext();
        var repo = new CategoryRepository(ctx);
        await repo.AddAsync(new Category { Name = "Zebra" });
        await repo.AddAsync(new Category { Name = "Apple" });
        await repo.SaveChangesAsync();

        var list = await repo.GetAllAsync();

        list.Select(c => c.Name).Should().Equal("Apple", "Zebra");
    }

    [Fact]
    public async Task CategoryRepository_DeleteAsync_soft_deletes()
    {
        await using var ctx = CreateContext();
        var repo = new CategoryRepository(ctx);
        var cat = new Category { Name = "Temp" };
        await repo.AddAsync(cat);
        await repo.SaveChangesAsync();

        await repo.DeleteAsync(cat);
        await repo.SaveChangesAsync();

        cat.IsDeleted.Should().BeTrue();
        cat.DeletedAt.Should().NotBeNull();

        var visible = await repo.GetAllAsync();
        visible.Should().BeEmpty();
    }

    [Fact]
    public async Task CategoryRepository_GetByIdAsync_returns_entity()
    {
        await using var ctx = CreateContext();
        var repo = new CategoryRepository(ctx);
        await repo.AddAsync(new Category { Name = "One" });
        await repo.SaveChangesAsync();
        var id = (await repo.GetAllAsync())[0].Id;

        var found = await repo.GetByIdAsync(id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("One");
    }
}
