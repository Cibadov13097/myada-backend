using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Seed;

/// <summary>Demo seed data for ADA University (Baku, Azerbaijan).</summary>
public static class LostFoundDbSeeder
{
    private static string SeedUserId(int index) =>
        Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}").ToString();

    public static async Task SeedAsync(LostFoundDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var now = DateTime.UtcNow;
            var names = new[]
            {
                "Electronics & gadgets",
                "ID cards, keys & wallets",
                "Books & course materials",
                "Clothing & bags",
                "Sports & water bottles",
                "Other"
            };

            foreach (var name in names)
            {
                await db.Categories.AddAsync(
                    new Category { Name = name, CreatedAt = now },
                    cancellationToken).ConfigureAwait(false);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var categoryByName = await db.Categories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Name, c => c.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!await db.LostItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var electronics = categoryByName["Electronics & gadgets"];
            var idCat = categoryByName["ID cards, keys & wallets"];
            var books = categoryByName["Books & course materials"];
            var clothing = categoryByName["Clothing & bags"];

            var lost = new[]
            {
                new LostItem
                {
                    Title = "Black laptop sleeve (ADA logo)",
                    Description =
                        "Left on a bench outside the Learning Hub. May contain COMP 201 printed notes.",
                    PostedByUserId = SeedUserId(3),
                    CategoryId = electronics,
                    BuildingId = null,
                    RoomId = null,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new LostItem
                {
                    Title = "Student ID on blue lanyard",
                    Description =
                        "Lost between the Founder's Building entrance and the shuttle pickup area on campus.",
                    PostedByUserId = SeedUserId(7),
                    CategoryId = idCat,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new LostItem
                {
                    Title = "Calculus textbook (sticker on spine)",
                    Description = "Left in Library Group Study Room 2 after a MATH 151 session.",
                    PostedByUserId = SeedUserId(12),
                    CategoryId = books,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                },
                new LostItem
                {
                    Title = "Grey hoodie with small coffee stain",
                    Description = "Forgotten in Kathleen Velasco Math & Science Building, near room 204.",
                    PostedByUserId = SeedUserId(22),
                    CategoryId = clothing,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
                }
            };

            await db.LostItems.AddRangeAsync(lost, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.FoundItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var electronics = categoryByName["Electronics & gadgets"];
            var idCat = categoryByName["ID cards, keys & wallets"];
            var other = categoryByName["Other"];

            var found = new[]
            {
                new FoundItem
                {
                    Description =
                        "Wireless earbuds in a small white charging case. Turned in at the Library front desk.",
                    FoundByUserId = SeedUserId(4),
                    CategoryId = electronics,
                    Status = Status.pending,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new FoundItem
                {
                    Description =
                        "House keys with a red SOCAR-branded keychain. Found near the Student Center entrance.",
                    FoundByUserId = SeedUserId(9),
                    CategoryId = idCat,
                    Status = Status.approved,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new FoundItem
                {
                    Description =
                        "Metal water bottle with stickers from Baku Marathon and ADA Orientation Week.",
                    FoundByUserId = SeedUserId(15),
                    CategoryId = other,
                    Status = Status.pending,
                    ImageUrl = string.Empty,
                    CreatedAt = DateTime.UtcNow.AddHours(-30)
                }
            };

            await db.FoundItems.AddRangeAsync(found, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
