using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LostFoundService.Infrastructure.Persistence.Seed;

/// <summary>Demo seed data for Lost &amp; Found screens.</summary>
public static class LostFoundDbSeeder
{
    private static readonly string[] RequiredCategoryNames =
    {
        "Electronics",
        "Documents",
        "Clothing",
        "Accessories",
        "Other",
        "Personal Items"
    };

    private static string SeedUserId(int index) =>
        Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}").ToString();

    public static async Task SeedAsync(LostFoundDbContext db, CancellationToken cancellationToken = default)
    {
        var categoryIdByName = await EnsureCategoriesAsync(db, cancellationToken).ConfigureAwait(false);

        async Task<int> GetCategoryIdAsync(string categoryName)
        {
            if (categoryIdByName.TryGetValue(categoryName, out var existingId))
            {
                return existingId;
            }

            var created = new Category
            {
                Name = categoryName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await db.Categories.AddAsync(created, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            categoryIdByName[created.Name] = created.Id;
            return created.Id;
        }

        var electronics = await GetCategoryIdAsync("Electronics").ConfigureAwait(false);
        var documents = await GetCategoryIdAsync("Documents").ConfigureAwait(false);
        var accessories = await GetCategoryIdAsync("Accessories").ConfigureAwait(false);

        await SeedLegacyLostItemsAsync(db, electronics, accessories, cancellationToken).ConfigureAwait(false);
        await SeedLegacyFoundItemsAsync(db, electronics, documents, accessories, cancellationToken).ConfigureAwait(false);
        await SeedUnifiedItemsAsync(db, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, int>> EnsureCategoriesAsync(
        LostFoundDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existingCategories = await db.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingNameSet = new HashSet<string>(
            existingCategories.Select(x => x.Name.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var missingCategoryNames = RequiredCategoryNames
            .Where(name => !existingNameSet.Contains(name))
            .ToList();

        if (missingCategoryNames.Count > 0)
        {
            foreach (var name in missingCategoryNames)
            {
                await db.Categories.AddAsync(new Category
                {
                    Name = name,
                    CreatedAt = now
                }, cancellationToken).ConfigureAwait(false);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var categoryByName = await db.Categories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return categoryByName
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SeedLegacyLostItemsAsync(
        LostFoundDbContext db,
        int electronicsCategoryId,
        int accessoriesCategoryId,
        CancellationToken cancellationToken)
    {
        if (await db.LostItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var lostItems = new[]
        {
            new LostItem
            {
                Title = "USB Flash Drive",
                Description = "SanDisk 64GB flash drive with a blue cap. Contains important project files.",
                PostedByUserId = SeedUserId(10),
                CategoryId = electronicsCategoryId,
                ImageUrl = "https://picsum.photos/seed/lost-usb-flash-drive/1000/700",
                CreatedAt = now.AddHours(-8)
            },
            new LostItem
            {
                Title = "Red Notebook",
                Description = "Red spiral notebook with lecture notes from the reading hall.",
                PostedByUserId = SeedUserId(11),
                CategoryId = accessoriesCategoryId,
                ImageUrl = "https://picsum.photos/seed/lost-red-notebook/1000/700",
                CreatedAt = now.AddHours(-7)
            },
            new LostItem
            {
                Title = "Silver MacBook Charger",
                Description = "61W USB-C charger with a silver extension cable adapter.",
                PostedByUserId = SeedUserId(12),
                CategoryId = electronicsCategoryId,
                ImageUrl = "https://picsum.photos/seed/lost-macbook-charger/1000/700",
                CreatedAt = now.AddHours(-6)
            },
            new LostItem
            {
                Title = "iPhone 14 Pro",
                Description = "Space black iPhone 14 Pro with clear case and minor scratch near the camera.",
                PostedByUserId = SeedUserId(13),
                CategoryId = electronicsCategoryId,
                ImageUrl = "https://picsum.photos/seed/lost-iphone-14-pro/1000/700",
                CreatedAt = now.AddHours(-5)
            }
        };

        await db.LostItems.AddRangeAsync(lostItems, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedLegacyFoundItemsAsync(
        LostFoundDbContext db,
        int electronicsCategoryId,
        int documentsCategoryId,
        int accessoriesCategoryId,
        CancellationToken cancellationToken)
    {
        if (await db.FoundItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var foundItems = new[]
        {
            new FoundItem
            {
                Description = "Prescription glasses found in Main Building - Room A301.",
                FoundByUserId = SeedUserId(20),
                CategoryId = accessoriesCategoryId,
                Status = Status.approved,
                ImageUrl = "https://picsum.photos/seed/found-prescription-glasses/1000/700",
                CreatedAt = now.AddHours(-4)
            },
            new FoundItem
            {
                Description = "House keys with two metal keys and one key fob found at Parking Area B.",
                FoundByUserId = SeedUserId(21),
                CategoryId = documentsCategoryId,
                Status = Status.approved,
                ImageUrl = "https://picsum.photos/seed/found-house-keys/1000/700",
                CreatedAt = now.AddHours(-3)
            },
            new FoundItem
            {
                Description = "AirPods Pro charging case found on a bench in the main yard.",
                FoundByUserId = SeedUserId(22),
                CategoryId = electronicsCategoryId,
                Status = Status.pending,
                ImageUrl = "https://picsum.photos/seed/found-airpods-case/1000/700",
                CreatedAt = now.AddHours(-2)
            }
        };

        await db.FoundItems.AddRangeAsync(foundItems, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedUnifiedItemsAsync(
        LostFoundDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.LostFoundItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var baseDate = new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc);
        var collectionDesk = "Lost & Found Office - Main Building, room A012";

        var items = new List<LostFoundItem>
        {
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "USB Flash Drive",
                Location = "Building C - Room C203",
                Category = "Electronics",
                Status = LostFoundPublicStatus.PendingVerification,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "SanDisk 64GB flash drive with a blue cap. Contains important project files.",
                ReferenceNumber = "LF-000010",
                PostedAt = baseDate,
                PrimaryImageUrl = "https://picsum.photos/seed/lf-usb-flash-drive/1000/700",
                PostedByUserId = SeedUserId(101),
                LocationType = "building",
                BuildingCode = "Building C",
                IsRoom = "true",
                RoomOrArea = "Room C203",
                Building = "Building C",
                RoomArea = "Room C203",
                DateLost = new DateOnly(2026, 4, 23),
                TimeLost = "17:40",
                ContactName = "A. Student",
                ContactPhone = "+994-55-000-0010",
                CreatedAt = baseDate
            },
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "Red Notebook",
                Location = "Library - 3rd Floor, reading hall near Window 4",
                Category = "Accessories",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "Red notebook with handwritten CS notes and a weekly timetable.",
                ReferenceNumber = "LF-000011",
                PostedAt = baseDate.AddHours(-1),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-red-notebook/1000/700",
                PostedByUserId = SeedUserId(102),
                LocationType = "building",
                BuildingCode = "Library",
                IsRoom = "true",
                RoomOrArea = "3rd Floor Reading Hall",
                Building = "Library",
                Floor = "3",
                RoomArea = "Reading Hall",
                DateLost = new DateOnly(2026, 4, 22),
                TimeLost = "12:20",
                ContactName = "B. Student",
                ContactPhone = "+994-55-000-0011",
                CreatedAt = baseDate.AddHours(-1)
            },
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "Silver MacBook Charger",
                Location = "Campus - Outdoor seating area between Building A and B",
                Category = "Electronics",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "Apple 61W USB-C MacBook charger with silver braided cable.",
                ReferenceNumber = "LF-000012",
                PostedAt = baseDate.AddHours(-2),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-silver-macbook-charger/1000/700",
                PostedByUserId = SeedUserId(103),
                LocationType = "campus",
                CampusLocation = "Outdoor seating area between Building A and B",
                LastKnownLocation = "Campus - Outdoor seating area between Building A and B",
                DateLost = new DateOnly(2026, 4, 22),
                TimeLost = "16:00",
                ContactName = "C. Student",
                ContactPhone = "+994-55-000-0012",
                CreatedAt = baseDate.AddHours(-2)
            },
            new()
            {
                Type = LostFoundItemType.Found,
                Title = "Prescription Glasses",
                Location = "Main Building - Room A301",
                Category = "Accessories",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received,
                Description = "Black-framed prescription glasses found on a desk in Room A301.",
                ReferenceNumber = "LF-000013",
                PostedAt = baseDate.AddHours(-3),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-prescription-glasses/1000/700",
                PostedByUserId = SeedUserId(104),
                LocationType = "building",
                BuildingCode = "Main Building",
                IsRoom = "true",
                RoomOrArea = "Room A301",
                Building = "Main Building",
                RoomArea = "Room A301",
                CollectionPlace = collectionDesk,
                StorageBinId = "BIN-A3-11",
                Condition = LostFoundItemCondition.Good,
                ConfirmAccuracy = true,
                CreatedAt = baseDate.AddHours(-3)
            },
            new()
            {
                Type = LostFoundItemType.Found,
                Title = "House Keys",
                Location = "Campus - Parking Area B, ground level",
                Category = "Accessories",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received,
                Description = "Set of house keys with a black key cover and small flashlight keychain.",
                ReferenceNumber = "LF-000014",
                PostedAt = baseDate.AddHours(-4),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-house-keys/1000/700",
                PostedByUserId = SeedUserId(105),
                LocationType = "campus",
                CampusLocation = "Parking Area B, ground level",
                LastKnownLocation = "Campus - Parking Area B, ground level",
                CollectionPlace = collectionDesk,
                StorageBinId = "BIN-P1-03",
                Condition = LostFoundItemCondition.Good,
                ConfirmAccuracy = true,
                CreatedAt = baseDate.AddHours(-4)
            },
            new()
            {
                Type = LostFoundItemType.Found,
                Title = "AirPods Pro Case",
                Location = "Campus - Main yard, on the bench near fountain",
                Category = "Electronics",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received,
                Description = "White AirPods Pro charging case with minor scratches on the lid.",
                ReferenceNumber = "LF-000015",
                PostedAt = baseDate.AddHours(-5),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-airpods-pro-case/1000/700",
                PostedByUserId = SeedUserId(106),
                LocationType = "campus",
                CampusLocation = "Main yard, bench near fountain",
                LastKnownLocation = "Campus - Main yard, on the bench near fountain",
                CollectionPlace = collectionDesk,
                StorageBinId = "BIN-E2-09",
                Condition = LostFoundItemCondition.Fair,
                ConfirmAccuracy = true,
                CreatedAt = baseDate.AddHours(-5)
            },
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "iPhone 14 Pro",
                Location = "Main Building - Room A120",
                Category = "Electronics",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "Space black iPhone 14 Pro in clear case, lost after afternoon lecture.",
                ReferenceNumber = "LF-000016",
                PostedAt = baseDate.AddHours(-6),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-iphone-14-pro/1000/700",
                PostedByUserId = SeedUserId(107),
                LocationType = "building",
                BuildingCode = "Main Building",
                IsRoom = "true",
                RoomOrArea = "Room A120",
                Building = "Main Building",
                RoomArea = "Room A120",
                DateLost = new DateOnly(2026, 4, 21),
                TimeLost = "15:10",
                ContactName = "D. Student",
                ContactPhone = "+994-55-000-0016",
                CreatedAt = baseDate.AddHours(-6)
            },
            new()
            {
                Type = LostFoundItemType.Found,
                Title = "Student ID Card",
                Location = "Student Center - Main entrance",
                Category = "Documents",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received,
                Description = "ADA student ID card found near the turnstiles.",
                ReferenceNumber = "LF-000017",
                PostedAt = baseDate.AddHours(-7),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-student-id-card/1000/700",
                PostedByUserId = SeedUserId(108),
                LocationType = "building",
                BuildingCode = "Student Center",
                IsRoom = "false",
                RoomOrArea = "Main entrance",
                Building = "Student Center",
                RoomArea = "Main entrance",
                CollectionPlace = collectionDesk,
                StorageBinId = "BIN-D1-01",
                Condition = LostFoundItemCondition.Good,
                ConfirmAccuracy = true,
                CreatedAt = baseDate.AddHours(-7)
            },
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "Blue Hoodie",
                Location = "Library - 2nd Floor, sofa area",
                Category = "Personal Items",
                Status = LostFoundPublicStatus.PendingVerification,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "Blue hoodie with white ADA logo on the sleeve.",
                ReferenceNumber = "LF-000018",
                PostedAt = baseDate.AddHours(-8),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-blue-hoodie/1000/700",
                PostedByUserId = SeedUserId(109),
                LocationType = "building",
                BuildingCode = "Library",
                IsRoom = "true",
                RoomOrArea = "2nd Floor sofa area",
                Building = "Library",
                Floor = "2",
                RoomArea = "Sofa area",
                DateLost = new DateOnly(2026, 4, 20),
                TimeLost = "19:25",
                ContactName = "E. Student",
                ContactPhone = "+994-55-000-0018",
                CreatedAt = baseDate.AddHours(-8)
            },
            new()
            {
                Type = LostFoundItemType.Lost,
                Title = "Brown Wallet",
                Location = "Campus - Cafeteria cashier counter",
                Category = "Documents",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Pending,
                Description = "Brown leather wallet with cards and a student transport pass.",
                ReferenceNumber = "LF-000019",
                PostedAt = baseDate.AddHours(-9),
                PrimaryImageUrl = "https://picsum.photos/seed/lf-brown-wallet/1000/700",
                PostedByUserId = SeedUserId(110),
                LocationType = "campus",
                CampusLocation = "Cafeteria cashier counter",
                LastKnownLocation = "Campus - Cafeteria cashier counter",
                DateLost = new DateOnly(2026, 4, 20),
                TimeLost = "13:40",
                ContactName = "F. Student",
                ContactPhone = "+994-55-000-0019",
                CreatedAt = baseDate.AddHours(-9)
            }
        };

        await db.LostFoundItems.AddRangeAsync(items, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var images = items
            .Where(item => !string.IsNullOrWhiteSpace(item.PrimaryImageUrl))
            .Select(item => new LostFoundItemImage
            {
                LostFoundItemId = item.Id,
                Url = item.PrimaryImageUrl!,
                SortOrder = 0
            })
            .ToList();

        if (images.Count == 0)
        {
            return;
        }

        await db.LostFoundItemImages.AddRangeAsync(images, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
