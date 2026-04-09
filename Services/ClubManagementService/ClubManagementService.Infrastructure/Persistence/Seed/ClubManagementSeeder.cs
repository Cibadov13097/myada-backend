using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Seed;

public static class ClubManagementSeeder
{
    public static async Task SeedAsync(ClubManagementDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            var categoryNames = new[]
            {
                "Technology",
                "Arts & Culture",
                "Business & Leadership",
                "Sports & Wellness",
                "Social Impact",
                "Science",
                "Media & Communications",
                "Entrepreneurship",
                "Health & Inclusion",
                "Sustainability"
            };

            var categories = categoryNames
                .Select((name, i) => new Category
                {
                    Name = name,
                    CreatedByUserId = BuildSeedUserGuid((i % 10) + 1)
                })
                .ToList();

            await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var categoryIds = await dbContext.Categories
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!await dbContext.Clubs.AnyAsync(cancellationToken))
        {
            var clubNamesFromPdf = new[]
            {
                "ART CLUB",
                "DANCE CLUB",
                "MUSIC CLUB",
                "CAREER CLUB",
                "CINEMATES CLUB",
                "MUTUAL SUPPORT CLUB",
                "BOOK CLUB",
                "EMPOWHERMENT CLUB",
                "EXCHANGE CLUB",
                "PHOTOGRAPHY CLUB",
                "INTELLECTUAL GAMES CLUB",
                "AGRINOVATION CLUB",
                "SOCIAL CLUB",
                "SPORTS CLUB",
                "CHARITY CLUB",
                "FILMOGRAPHY CLUB",
                "MEDICAL CLUB",
                "THE BANKERS CLUB",
                "COMICVERSE CLUB",
                "E-COMMERCE CLUB",
                "IEEE CLUB",
                "POLITICS CLUB",
                "FASHION CLUB",
                "CHESS CLUB",
                "ARCHITECTURE CLUB",
                "SAKURA CLUB",
                "DESIGN CLUB",
                "CLOUD CLUB",
                "MEDIA CLUB",
                "MUN CLUB",
                "TOPCHUBASHOV FELLOWSHIP CLUB"
            };

            var clubs = clubNamesFromPdf
                .Select((clubName, idx) => new Club
                {
                    Name = clubName,
                    Description = $"Official ADA club '{clubName}' seeded for integration testing.",
                    ApprovedBy = BuildSeedUserGuid((idx % 50) + 1),
                    Status = (ClubStatus)(idx % 4),
                    CategoryId = categoryIds[idx % categoryIds.Count],
                    CreatedByUserId = BuildSeedUserGuid((idx % 50) + 1)
                })
                .ToList();

            await dbContext.Clubs.AddRangeAsync(clubs, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Events.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var events = Enumerable.Range(1, 10)
                .Select(i => new Event
                {
                    Name = $"Seed Event {i}",
                    Description = $"Seeded event {i} for ClubManagement module testing.",
                    StartTime = now.AddDays(i).AddHours(9),
                    EndTime = now.AddDays(i).AddHours(11),
                    Location = $"Campus Hall {(i - 1) % 5 + 1}",
                    SeatLimit = 40 + i,
                    Requirements = "Student ID",
                    Prerequisites = "None",
                    Notes = "Auto-seeded event",
                    ApprovedBy = BuildSeedUserGuid(((i - 1) % 15) + 1),
                    Status = (ClubStatus)((i - 1) % 4),
                    CreatedByUserId = BuildSeedUserGuid(((i - 1) % 15) + 1)
                })
                .ToList();

            await dbContext.Events.AddRangeAsync(events, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.EventRegistrations.AnyAsync(cancellationToken))
        {
            var registrations = Enumerable.Range(1, 10)
                .Select(i => new EventRegistration
                {
                    EventId = Guid.NewGuid(),
                    UserId = BuildSeedUserGuid(i),
                    CreatedByUserId = BuildSeedUserGuid(i)
                })
                .ToList();

            await dbContext.EventRegistrations.AddRangeAsync(registrations, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var clubIds = await dbContext.Clubs
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!await dbContext.JobVacancies.AnyAsync(cancellationToken))
        {
            var vacancies = Enumerable.Range(1, 10)
                .Select(i => new JobVacancy
                {
                    ClubId = clubIds[(i - 1) % clubIds.Count],
                    Title = $"Seed Vacancy {i}",
                    Description = $"Seeded vacancy {i} for recruitment testing.",
                    PostedBy = BuildSeedUserGuid(i + 10),
                    CreatedByUserId = BuildSeedUserGuid(i + 10)
                })
                .ToList();

            await dbContext.JobVacancies.AddRangeAsync(vacancies, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var vacancyIds = await dbContext.JobVacancies
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!await dbContext.JobApplications.AnyAsync(cancellationToken))
        {
            var applications = Enumerable.Range(1, 10)
                .Select(i => new JobApplication
                {
                    JobVacancyId = vacancyIds[(i - 1) % vacancyIds.Count],
                    ApplicantId = BuildSeedUserGuid(i + 20),
                    ExperienceLinks = $"https://portfolio.local/user{i + 20}",
                    FileUrl = $"https://files.local/cv/seed_user_{i + 20}.pdf",
                    Status = (JobApplicationStatus)((i - 1) % 4),
                    CreatedByUserId = BuildSeedUserGuid(i + 20)
                })
                .ToList();

            await dbContext.JobApplications.AddRangeAsync(applications, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var eventIds = await dbContext.Events
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!await dbContext.Subevents.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var subevents = Enumerable.Range(1, 10)
                .Select(i => new Subevent
                {
                    MainEventId = eventIds[(i - 1) % eventIds.Count],
                    Name = $"Seed Subevent {i}",
                    StartTime = now.AddDays(i).AddHours(12),
                    EndTime = now.AddDays(i).AddHours(13),
                    CreatedByUserId = BuildSeedUserGuid(i + 30)
                })
                .ToList();

            await dbContext.Subevents.AddRangeAsync(subevents, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static Guid BuildSeedUserGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}
