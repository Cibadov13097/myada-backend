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
        var now = DateTime.UtcNow;
        var requiredCategoryNames = new[]
        {
            "Electronics & gadgets",
            "ID cards, keys & wallets",
            "Books & course materials",
            "Clothing & bags",
            "Sports & water bottles",
            "Other"
        };

        var existingCategories = await db.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingNameSet = new HashSet<string>(
            existingCategories.Select(x => x.Name),
            StringComparer.OrdinalIgnoreCase);

        var missingCategoryNames = requiredCategoryNames
            .Where(name => !existingNameSet.Contains(name))
            .ToList();

        if (missingCategoryNames.Count > 0)
        {
            foreach (var name in missingCategoryNames)
            {
                await db.Categories.AddAsync(
                    new Category { Name = name, CreatedAt = now },
                    cancellationToken).ConfigureAwait(false);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var categoryByName = await db.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var categoryIdByName = categoryByName
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        async Task<int> GetCategoryIdAsync(params string[] preferredNames)
        {
            foreach (var name in preferredNames)
            {
                if (categoryIdByName.TryGetValue(name, out var existingId))
                {
                    return existingId;
                }
            }

            var created = new Category
            {
                Name = preferredNames[0],
                CreatedAt = DateTime.UtcNow
            };

            await db.Categories.AddAsync(created, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            categoryIdByName[created.Name] = created.Id;
            return created.Id;
        }

        var electronics = await GetCategoryIdAsync("Electronics & gadgets", "Electronics").ConfigureAwait(false);
        var idCat = await GetCategoryIdAsync("ID cards, keys & wallets", "Documents").ConfigureAwait(false);
        var books = await GetCategoryIdAsync("Books & course materials").ConfigureAwait(false);
        var clothing = await GetCategoryIdAsync("Clothing & bags", "Personal Items").ConfigureAwait(false);
        var other = await GetCategoryIdAsync("Other", "Accessories").ConfigureAwait(false);

        if (!await db.LostItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
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
