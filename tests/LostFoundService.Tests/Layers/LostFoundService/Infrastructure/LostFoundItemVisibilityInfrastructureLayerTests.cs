using FluentAssertions;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Infrastructure.Persistence.Context;
using LostFoundService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Tests.Layers.Infrastructure;

public sealed class LostFoundItemVisibilityInfrastructureLayerTests
{
    private static LostFoundDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LostFoundDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new LostFoundDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SearchItemsAsync_public_catalog_keeps_received_items_and_hides_delivered_items()
    {
        await using var context = CreateContext();
        var repository = new LostAndFoundRepository(context);
        await SeedCatalogAsync(context);

        var result = await repository.SearchItemsAsync(
            searchTerm: null,
            type: null,
            postedByUserId: null,
            categoryId: null,
            status: null,
            adminStatus: null,
            excludeTerminalAdminItems: true,
            page: 1,
            limit: 10);

        result.Items.Select(item => item.Title).Should().Contain(new[] { "Received Item", "Pending Item" });
        result.Items.Select(item => item.Title).Should().NotContain("Delivered Item");
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task CountCatalogItemsExcludingTerminalAsync_counts_received_items_as_public()
    {
        await using var context = CreateContext();
        var repository = new LostAndFoundRepository(context);
        await SeedCatalogAsync(context);

        var total = await repository.CountCatalogItemsExcludingTerminalAsync(publicStatus: null);
        var active = await repository.CountCatalogItemsExcludingTerminalAsync(LostFoundPublicStatus.Active);
        var pending = await repository.CountCatalogItemsExcludingTerminalAsync(LostFoundPublicStatus.PendingVerification);

        total.Should().Be(2);
        active.Should().Be(1);
        pending.Should().Be(1);
    }

    private static async Task SeedCatalogAsync(LostFoundDbContext context)
    {
        var category = new Category { Id = 1, Name = "Electronics" };
        context.Categories.Add(category);

        context.LostFoundItems.AddRange(
            new LostFoundItem
            {
                Id = 1,
                Title = "Received Item",
                CategoryId = category.Id,
                Category = category,
                Type = LostFoundItemType.Found,
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received,
                PostedAt = DateTime.UtcNow.AddMinutes(-2)
            },
            new LostFoundItem
            {
                Id = 2,
                Title = "Pending Item",
                CategoryId = category.Id,
                Category = category,
                Type = LostFoundItemType.Lost,
                Status = LostFoundPublicStatus.PendingVerification,
                AdminStatus = LostFoundAdminStatus.Pending,
                PostedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new LostFoundItem
            {
                Id = 3,
                Title = "Delivered Item",
                CategoryId = category.Id,
                Category = category,
                Type = LostFoundItemType.Found,
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Delivered,
                PostedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }
}
