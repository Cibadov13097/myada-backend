using LocationService.Domain.Entities;
using LocationService.Domain.Enums;
using LocationService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace LocationService.Infrastructure.Persistence.Seed;

/// <summary>Demo buildings and rooms for ADA University (Baku main campus).</summary>
public static class LocationDbSeeder
{
    public static async Task SeedAsync(LocationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Buildings.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var now = DateTime.UtcNow;

        var founders = new Building
        {
            Name = "Founder's Building",
            CreatedAt = now,
            Rooms =
            {
                new Room
                {
                    Name = "Lecture Hall A",
                    Number = "FB-101",
                    Capacity = 120,
                    RoomType = RoomType.Auditorium,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Seminar Room 3",
                    Number = "FB-203",
                    Capacity = 35,
                    RoomType = RoomType.Classroom,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Faculty Office Suite",
                    Number = "FB-310",
                    Capacity = 8,
                    RoomType = RoomType.Office,
                    CreatedAt = now
                }
            }
        };

        var learningHub = new Building
        {
            Name = "Learning Hub",
            CreatedAt = now,
            Rooms =
            {
                new Room
                {
                    Name = "Collaborative Study Zone",
                    Number = "LH-G01",
                    Capacity = 40,
                    RoomType = RoomType.Other,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Group Study Room 2",
                    Number = "LH-202",
                    Capacity = 10,
                    RoomType = RoomType.Other,
                    CreatedAt = now
                }
            }
        };

        var library = new Building
        {
            Name = "ADA Library",
            CreatedAt = now,
            Rooms =
            {
                new Room
                {
                    Name = "Quiet Reading Area",
                    Number = "LIB-1",
                    Capacity = 50,
                    RoomType = RoomType.Other,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Digital Media Lab",
                    Number = "LIB-115",
                    Capacity = 24,
                    RoomType = RoomType.Lab,
                    CreatedAt = now
                }
            }
        };

        var mathScience = new Building
        {
            Name = "Kathleen Velasco Math & Science Building",
            CreatedAt = now,
            Rooms =
            {
                new Room
                {
                    Name = "Physics Lab",
                    Number = "MSB-201",
                    Capacity = 22,
                    RoomType = RoomType.Lab,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Classroom 305",
                    Number = "MSB-305",
                    Capacity = 45,
                    RoomType = RoomType.Classroom,
                    CreatedAt = now
                }
            }
        };

        var shamakhi = new Building
        {
            Name = "ADA Residential Complex (Shamakhi)",
            CreatedAt = now,
            Rooms =
            {
                new Room
                {
                    Name = "Common Lounge — Block A",
                    Number = "SHM-A-L1",
                    Capacity = 30,
                    RoomType = RoomType.Other,
                    CreatedAt = now
                },
                new Room
                {
                    Name = "Study Room — Block B",
                    Number = "SHM-B-12",
                    Capacity = 12,
                    RoomType = RoomType.Classroom,
                    CreatedAt = now
                }
            }
        };

        await db.Buildings
            .AddRangeAsync(new[] { founders, learningHub, library, mathScience, shamakhi }, cancellationToken)
            .ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
