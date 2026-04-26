using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.Context;
using AuthService.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Tests.Layers.Infrastructure;

/// <summary>Medium service — infrastructure layer guideline ~5–20 tests.</summary>
public sealed class RefreshTokenRepositoryLayerTests
{
    private static MyAdaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyAdaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new MyAdaDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task GetByTokenAsync_returns_null_when_missing()
    {
        await using var ctx = CreateContext();
        var repo = new RefreshTokenRepository(ctx);

        var found = await repo.GetByTokenAsync("missing-token");

        found.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_then_GetByTokenAsync_returns_entity()
    {
        await using var ctx = CreateContext();
        var repo = new RefreshTokenRepository(ctx);
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "abc",
            UserId = "user-1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);

        var found = await repo.GetByTokenAsync("abc");
        found.Should().NotBeNull();
        found!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        await using var ctx = CreateContext();
        var repo = new RefreshTokenRepository(ctx);
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "tok",
            UserId = "u",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(entity);

        entity.RevokedAt = DateTime.UtcNow;
        await repo.UpdateAsync(entity);

        var found = await repo.GetByTokenAsync("tok");
        found!.RevokedAt.Should().NotBeNull();
    }
}
