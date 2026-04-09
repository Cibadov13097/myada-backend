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
            var categories = new List<Category>
            {
                new() { Name = "Technology" },
                new() { Name = "Arts & Culture" },
                new() { Name = "Business & Leadership" },
                new() { Name = "Sports & Wellness" },
                new() { Name = "Social Impact" }
            };

            await dbContext.Categories.AddRangeAsync(categories, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var categoryMap = await dbContext.Categories
            .AsNoTracking()
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Id = g.OrderBy(x => x.Id).Select(x => x.Id).First() })
            .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

        if (!await dbContext.Clubs.AnyAsync(cancellationToken))
        {
            var clubs = new List<Club>
            {
                new()
                {
                    Name = "ADA Digital Entertainment Club",
                    Description = "Community for game design, esports production, streaming, and digital storytelling projects.",
                    ApprovedBy = Guid.Parse("4ec938de-7677-43d3-baa7-f3146f6cf111"),
                    Status = ClubStatus.Approved,
                    CategoryId = categoryMap["Technology"]
                },
                new()
                {
                    Name = "FinTech & Product Builders Club",
                    Description = "Students build product case studies and prototypes at the intersection of finance and technology.",
                    ApprovedBy = Guid.Parse("f91357b1-3598-4dce-822f-b88715f2f222"),
                    Status = ClubStatus.Approved,
                    CategoryId = categoryMap["Business & Leadership"]
                },
                new()
                {
                    Name = "Modern Arts Society",
                    Description = "Collaborative space for visual arts, photography, design critique sessions, and exhibitions.",
                    ApprovedBy = Guid.Parse("0cbf1efd-ee53-4ef0-8a7f-8dcf7f44b333"),
                    Status = ClubStatus.Pending,
                    CategoryId = categoryMap["Arts & Culture"]
                },
                new()
                {
                    Name = "Campus Active Life Club",
                    Description = "Promotes student well-being through weekly fitness sessions, running groups, and wellness talks.",
                    ApprovedBy = Guid.Parse("8a55b73e-6ab1-4856-9d2e-d3656afca444"),
                    Status = ClubStatus.Approved,
                    CategoryId = categoryMap["Sports & Wellness"]
                },
                new()
                {
                    Name = "Community Service Network",
                    Description = "Organizes volunteering programs and social impact campaigns with local partners.",
                    ApprovedBy = Guid.Parse("52f31af5-0f67-4880-95eb-c89eac08b555"),
                    Status = ClubStatus.Approved,
                    CategoryId = categoryMap["Social Impact"]
                }
            };

            await dbContext.Clubs.AddRangeAsync(clubs, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var clubMap = await dbContext.Clubs
            .AsNoTracking()
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Id = g.OrderBy(x => x.Id).Select(x => x.Id).First() })
            .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

        if (!await dbContext.Events.AnyAsync(cancellationToken))
        {
            var events = new List<Event>
            {
                new()
                {
                    Name = "Esports Production Bootcamp",
                    Description = "Hands-on workshop on tournament setup, stream overlays, and match-day operations.",
                    StartTime = DateTime.UtcNow.AddDays(10),
                    EndTime = DateTime.UtcNow.AddDays(10).AddHours(3),
                    Location = "Building B - Innovation Lab",
                    SeatLimit = 80,
                    Requirements = "Student ID and laptop",
                    Prerequisites = "Basic OBS or streaming familiarity",
                    Notes = "Bring headset if available.",
                    ApprovedBy = Guid.Parse("f91f1939-7d4c-4cca-ab2c-a9a1295d6111"),
                    Status = ClubStatus.Approved
                },
                new()
                {
                    Name = "Product Pitch Night",
                    Description = "Student teams pitch startup concepts to alumni mentors and industry guests.",
                    StartTime = DateTime.UtcNow.AddDays(16),
                    EndTime = DateTime.UtcNow.AddDays(16).AddHours(2),
                    Location = "Main Auditorium",
                    SeatLimit = 200,
                    Requirements = "Pre-registration",
                    Prerequisites = "None",
                    Notes = "Pitch deck deadline is 24 hours before event.",
                    ApprovedBy = Guid.Parse("cb563d5c-9d2e-49ee-b34c-819ee7b4d222"),
                    Status = ClubStatus.Approved
                },
                new()
                {
                    Name = "Spring Art Showcase",
                    Description = "Gallery-style exhibition featuring student illustration, photography, and mixed media.",
                    StartTime = DateTime.UtcNow.AddDays(22),
                    EndTime = DateTime.UtcNow.AddDays(22).AddHours(4),
                    Location = "Student Center Hall",
                    SeatLimit = 300,
                    Requirements = "Registration for exhibitors",
                    Prerequisites = "Submission approval for displayed work",
                    Notes = "Setup starts 2 hours before opening.",
                    ApprovedBy = Guid.Parse("fc730e38-5458-45f4-8649-c2ec5354b333"),
                    Status = ClubStatus.Pending
                },
                new()
                {
                    Name = "Community Volunteering Orientation",
                    Description = "Onboarding session for students joining semester-long social impact projects.",
                    StartTime = DateTime.UtcNow.AddDays(6),
                    EndTime = DateTime.UtcNow.AddDays(6).AddHours(2),
                    Location = "Room C-210",
                    SeatLimit = 120,
                    Requirements = "Valid student profile",
                    Prerequisites = "Interest form completed",
                    Notes = "Attendance required before field activities.",
                    ApprovedBy = Guid.Parse("59701dfd-13d7-4f59-bba4-08f0f7a1d444"),
                    Status = ClubStatus.Approved
                }
            };

            await dbContext.Events.AddRangeAsync(events, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var eventIds = await dbContext.Events
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(4)
            .ToListAsync(cancellationToken);

        if (!await dbContext.EventRegistrations.AnyAsync(cancellationToken) && eventIds.Count >= 2)
        {
            var registrations = new List<EventRegistration>
            {
                new() { EventId = Guid.NewGuid(), UserId = Guid.Parse("702f8ec9-94d4-42bf-b7fd-5b9e6810a111") },
                new() { EventId = Guid.NewGuid(), UserId = Guid.Parse("1c0260e9-16c0-4d4b-9f03-cf2d4c4ee222") },
                new() { EventId = Guid.NewGuid(), UserId = Guid.Parse("8de95dc4-8f8f-4a0d-8cab-eb2398fe9333") },
                new() { EventId = Guid.NewGuid(), UserId = Guid.Parse("77f2fdf2-4bce-4b4d-bf74-1f41dbf6d444") },
                new() { EventId = Guid.NewGuid(), UserId = Guid.Parse("9dd9376a-e3d1-4d3d-b438-8a55f4d51555") }
            };

            await dbContext.EventRegistrations.AddRangeAsync(registrations, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.JobVacancies.AnyAsync(cancellationToken))
        {
            var vacancies = new List<JobVacancy>
            {
                new()
                {
                    ClubId = clubMap["ADA Digital Entertainment Club"],
                    Title = "Broadcast Operations Coordinator",
                    Description = "Manage stream schedules, OBS scenes, and live match coordination for club tournaments.",
                    PostedBy = Guid.Parse("8da5f41e-9f1c-4cb9-96b1-b7d56c24a111")
                },
                new()
                {
                    ClubId = clubMap["ADA Digital Entertainment Club"],
                    Title = "Content Designer",
                    Description = "Create social media creatives, posters, and event visual packs.",
                    PostedBy = Guid.Parse("b0f3cf8f-6f5f-4f58-b348-0058dd5e4222")
                },
                new()
                {
                    ClubId = clubMap["FinTech & Product Builders Club"],
                    Title = "Research Analyst",
                    Description = "Support product benchmarking, user interviews, and data synthesis.",
                    PostedBy = Guid.Parse("3a7f8cf2-4ca7-4b97-8cd0-607f8a9ab333")
                },
                new()
                {
                    ClubId = clubMap["Community Service Network"],
                    Title = "Volunteer Program Coordinator",
                    Description = "Coordinate participant schedules and partner communication for outreach projects.",
                    PostedBy = Guid.Parse("145779bc-5624-4a89-b736-c16cc0848444")
                }
            };

            await dbContext.JobVacancies.AddRangeAsync(vacancies, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var vacancyIds = await dbContext.JobVacancies
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (!await dbContext.JobApplications.AnyAsync(cancellationToken) && vacancyIds.Count >= 2)
        {
            var applications = new List<JobApplication>
            {
                new()
                {
                    JobVacancyId = vacancyIds[0],
                    ApplicantId = Guid.Parse("3dd5fcbe-fc77-4c6f-a27f-0b5df65ff111"),
                    ExperienceLinks = "https://portfolio.example.com/nigar-portfolio;https://linkedin.com/in/nigar-student",
                    FileUrl = "https://files.example.com/cv/nigar_rahimova.pdf",
                    Status = JobApplicationStatus.Pending
                },
                new()
                {
                    JobVacancyId = vacancyIds[0],
                    ApplicantId = Guid.Parse("0a8f8b18-1d91-4d5a-879f-43db8d7ec222"),
                    ExperienceLinks = "https://portfolio.example.com/tural-streaming",
                    FileUrl = "https://files.example.com/cv/tural_aliyev.pdf",
                    Status = JobApplicationStatus.Approved
                },
                new()
                {
                    JobVacancyId = vacancyIds[1],
                    ApplicantId = Guid.Parse("7838e134-cd9a-4f4b-8e8d-365c7424d333"),
                    ExperienceLinks = "https://behance.net/lala-designs",
                    FileUrl = "https://files.example.com/cv/lala_guliyeva.pdf",
                    Status = JobApplicationStatus.Pending
                },
                new()
                {
                    JobVacancyId = vacancyIds[2],
                    ApplicantId = Guid.Parse("2f262f01-3ff1-45c8-bfbb-1e2ccf70b444"),
                    ExperienceLinks = "https://github.com/kamal-product-labs",
                    FileUrl = "https://files.example.com/cv/kamal_mammadov.pdf",
                    Status = JobApplicationStatus.Rejected
                },
                new()
                {
                    JobVacancyId = vacancyIds[3],
                    ApplicantId = Guid.Parse("853e35c3-f3db-4b95-91d2-6302c50dc555"),
                    ExperienceLinks = "https://portfolio.example.com/ayla-impact",
                    FileUrl = "https://files.example.com/cv/ayla_huseynli.pdf",
                    Status = JobApplicationStatus.Cancelled
                }
            };

            await dbContext.JobApplications.AddRangeAsync(applications, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Subevents.AnyAsync(cancellationToken) && eventIds.Count > 0)
        {
            var subevents = new List<Subevent>
            {
                new()
                {
                    MainEventId = eventIds[0],
                    Name = "Check-in and Team Setup",
                    StartTime = DateTime.UtcNow.AddDays(10),
                    EndTime = DateTime.UtcNow.AddDays(10).AddMinutes(30)
                },
                new()
                {
                    MainEventId = eventIds[0],
                    Name = "Live Production Simulation",
                    StartTime = DateTime.UtcNow.AddDays(10).AddMinutes(40),
                    EndTime = DateTime.UtcNow.AddDays(10).AddHours(1).AddMinutes(40)
                },
                new()
                {
                    MainEventId = eventIds[1],
                    Name = "Pitch Round 1",
                    StartTime = DateTime.UtcNow.AddDays(16).AddMinutes(20),
                    EndTime = DateTime.UtcNow.AddDays(16).AddHours(1).AddMinutes(10)
                },
                new()
                {
                    MainEventId = eventIds[1],
                    Name = "Mentor Feedback Session",
                    StartTime = DateTime.UtcNow.AddDays(16).AddHours(1).AddMinutes(20),
                    EndTime = DateTime.UtcNow.AddDays(16).AddHours(2)
                }
            };

            await dbContext.Subevents.AddRangeAsync(subevents, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
