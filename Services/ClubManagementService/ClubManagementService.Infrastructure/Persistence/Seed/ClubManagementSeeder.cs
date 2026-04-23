using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Seed;

/// <summary>Development/demo seed data themed for ADA University (Baku, Azerbaijan).</summary>
public static class ClubManagementSeeder
{
    public static async Task SeedAsync(ClubManagementDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken).ConfigureAwait(false);

        var categoryIds = await dbContext.Categories
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await SeedClubsAsync(dbContext, categoryIds, cancellationToken).ConfigureAwait(false);

        var clubIds = await dbContext.Clubs
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await SeedClubPositionsAsync(dbContext, clubIds, categoryIds, cancellationToken).ConfigureAwait(false);

        var positionIds = await dbContext.ClubPositions
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await SeedClubMembershipsAsync(dbContext, clubIds, cancellationToken).ConfigureAwait(false);
        await SeedEventsAsync(dbContext, clubIds, cancellationToken).ConfigureAwait(false);

        var eventIds = await dbContext.Events
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await SeedSubeventsAsync(dbContext, eventIds, cancellationToken).ConfigureAwait(false);
        await SeedEventRegistrationsAsync(dbContext, eventIds, cancellationToken).ConfigureAwait(false);
        await SeedJobVacanciesAsync(dbContext, clubIds, positionIds, cancellationToken).ConfigureAwait(false);

        var vacancyIds = await dbContext.JobVacancies
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await SeedJobApplicationsAsync(dbContext, vacancyIds, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedCategoriesAsync(ClubManagementDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Categories.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var categoryNames = new[]
        {
            "STEM & Computing",
            "Arts & Culture",
            "Business & Finance",
            "Sports & Recreation",
            "Social Impact & Volunteering",
            "Media & Communications",
            "Architecture & Design",
            "International Affairs",
            "Health & Wellbeing",
            "Entrepreneurship"
        };

        var categories = categoryNames
            .Select((name, i) => new Category
            {
                Name = name,
                CreatedByUserId = BuildSeedUserGuid((i % 10) + 1)
            })
            .ToList();

        await dbContext.Categories.AddRangeAsync(categories, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0 || await dbContext.Clubs.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var clubs = new[]
        {
            ("ADA Computer Science Society", "Workshops on algorithms, competitive programming, and hackathons at ADA University."),
            ("ADA IEEE Student Branch", "Technical talks, industry visits, and IEEE programming contests for engineering students."),
            ("ADA Model United Nations (MUN)", "Diplomacy simulations, public speaking, and preparation for international MUN conferences."),
            ("ADA Entrepreneurship Club", "Startup weekends, pitch nights, and mentoring with founders in Azerbaijan’s growing tech ecosystem."),
            ("ADA Photography & Visual Arts", "Campus life coverage, exhibitions, and collaboration with Arts & Culture events in Baku."),
            ("ADA Chess Club", "Rated tournaments, blitz nights, and friendly matches across schools at ADA."),
            ("ADA Dance Ensemble", "Contemporary and folk performances for university ceremonies and city cultural events."),
            ("ADA Music & Performance", "Open mics, ensemble practice, and support for ADA’s annual talent showcases."),
            ("Green ADA — Environmental Action", "Campus sustainability, recycling drives, and field trips to the Greater Caucasus foothills."),
            ("ADA Book & Literature Circle", "Discussions in English and Azerbaijani, author talks, and World Book Day celebrations."),
            ("ADA Film & Cinema Society", "Screenings, short-film festivals, and critique nights at ADA’s learning hub."),
            ("ADA Architecture & Urbanism Forum", "Sketch walks in Baku, guest lectures on Caspian waterfront development, and design charrettes."),
            ("ADA Women in Tech", "Mentoring circles, meetups with women leaders in ICT, and outreach to schools in Azerbaijan."),
            ("ADA Debate Society", "British parliamentary format training and inter-university tournaments in English."),
            ("ADA Volunteer Corps", "Community service in Baku neighbourhoods, blood drives, and support for refugee education NGOs."),
            ("ADA Sports Union", "Intramural football and basketball, fitness challenges, and Shamakhi campus outdoor activities."),
            ("ADA Turkish–Azerbaijani Cultural Exchange", "Language tables, Nowruz events, and joint programming with partner universities."),
            ("ADA Data Science & AI Club", "Kaggle teams, ML reading groups, and ethics discussions on AI in Azerbaijani public sector."),
            ("ADA Finance & Investment Society", "Bloomberg basics, CFA info sessions, and case studies on the Baku Stock Exchange."),
            ("ADA Psychology & Mental Health Advocates", "Peer listening, stress-management workshops during exam periods at ADA."),
            ("ADA International Students Association", "Welcome weeks, buddy programmes, and trips introducing newcomers to Azerbaijan."),
            ("ADA Azerbaijani Culture & Heritage", "Mugham evenings, carpet-weaving demos, and talks on UNESCO sites across the country."),
            ("ADA Robotics & Maker Lab", "ROS tutorials, drone regulations in AZ, and preparation for Eurobot-style competitions."),
            ("ADA Marketing & Design Studio", "Branding help for student startups, Canva/Figma clinics, and social media best practices.")
        };

        var rows = clubs
            .Select((pair, idx) => new Club
            {
                Name = pair.Item1,
                Description = pair.Item2,
                ApprovedBy = BuildSeedUserGuid((idx % 40) + 1),
                Status = idx % 7 == 0 ? ClubStatus.Pending : ClubStatus.Approved,
                CategoryId = categoryIds[idx % categoryIds.Count],
                CreatedByUserId = BuildSeedUserGuid((idx % 40) + 1)
            })
            .ToList();

        await dbContext.Clubs.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubPositionsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> clubIds,
        IReadOnlyList<int> categoryIds,
        CancellationToken cancellationToken)
    {
        if (clubIds.Count == 0 || categoryIds.Count == 0 ||
            await dbContext.ClubPositions.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var templates = new (int ClubIndex, string Title, int CategoryIndex, string[] Requirements)[]
        {
            (0, "Outreach Coordinator", 0, new[]
            {
                "Strong written English; Azerbaijani is a plus for local school visits.",
                "Comfort speaking to prospective students at ADA open days."
            }),
            (0, "Technical Workshop Lead", 0, new[]
            {
                "Completed CS201 or equivalent; experience mentoring peers.",
                "Availability for two evening sessions per month on campus."
            }),
            (1, "Events Vice-Chair", 5, new[]
            {
                "Organized at least one medium-sized student event before.",
                "Familiarity with ADA room-booking and safety guidelines."
            }),
            (2, "Delegate Training Officer", 7, new[]
            {
                "Prior MUN or debate experience; research skills using UN documents.",
                "Willingness to coach first-time delegates in English."
            }),
            (3, "Startup Pitch Curator", 9, new[]
            {
                "Interest in Azerbaijan’s startup ecosystem and early-stage investing.",
                "Can commit to screening 5–8 pitch decks per semester."
            }),
            (4, "Exhibition Coordinator", 1, new[]
            {
                "Portfolio of photography or digital art; respect for model releases.",
                "Experience with print labs or gallery hanging in Baku is helpful."
            }),
            (5, "Tournament Director", 3, new[]
            {
                "National chess federation ID or ELO > 1500 preferred.",
                "Fair-play mindset and knowledge of FIDE rapid rules."
            }),
            (6, "Choreography Assistant", 1, new[]
            {
                "Dance background (folk or contemporary); ADA sports clearance form.",
                "Teamwork during rehearsals before Founders’ Day and graduation."
            }),
            (7, "Sound & Stage Manager", 5, new[]
            {
                "Basic live-sound mixing; can coordinate with ADA facilities office.",
                "Evening availability for concerts in the student center."
            }),
            (8, "Sustainability Metrics Analyst", 4, new[]
            {
                "Interest in campus waste audits and carbon footprint tracking.",
                "Spreadsheet skills; environmental science or engineering major preferred."
            }),
            (9, "Reading Group Facilitator", 1, new[]
            {
                "Leads one monthly discussion in English; Azerbaijani titles welcome.",
                "Coordinates with ADA library for room and display copies."
            }),
            (10, "Projection & Subtitles Lead", 5, new[]
            {
                "Care for AV equipment; subtitles in AZ/EN/TR as needed for screenings.",
                "Copyright awareness for licensed films."
            }),
            (11, "Urban Sketching Captain", 6, new[]
            {
                "Sketchbooks from Icherisheher and modern Baku waterfront walks.",
                "Shares supplies with beginners; posts highlights on club socials."
            }),
            (12, "Mentorship Pairing Coordinator", 0, new[]
            {
                "Matches mentors and mentees across ADA schools; GDPR-style discretion.",
                "One-year commitment with mid-semester check-ins."
            }),
            (13, "Community Project Liaison", 4, new[]
            {
                "Fluent Azerbaijani for NGO communication; valid volunteer insurance.",
                "Documents hours for ADA’s service-learning recognition."
            }),
            (14, "Intramurals Referee", 3, new[]
            {
                "Fair officiating; first-aid awareness; weekend slots at ADA gym.",
                "Respects ADA code of conduct for spectators and players."
            })
        };

        var positions = new List<ClubPosition>();
        foreach (var t in templates)
        {
            var clubIndex = Math.Min(t.ClubIndex, clubIds.Count - 1);
            var catIndex = Math.Min(t.CategoryIndex, categoryIds.Count - 1);
            var pos = new ClubPosition
            {
                ClubId = clubIds[clubIndex],
                Title = t.Title,
                CategoryId = categoryIds[catIndex],
                CreatedByUserId = BuildSeedUserGuid(200 + positions.Count)
            };
            var order = 0;
            foreach (var line in t.Requirements)
            {
                pos.Requirements.Add(new ClubPositionRequirement
                {
                    SortOrder = order++,
                    Text = line
                });
            }

            positions.Add(pos);
        }

        await dbContext.ClubPositions.AddRangeAsync(positions, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubMembershipsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> clubIds,
        CancellationToken cancellationToken)
    {
        if (clubIds.Count == 0 || await dbContext.ClubMemberships.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var memberships = new List<ClubMembership>();
        var used = new HashSet<(int ClubId, Guid UserId)>();
        var userStart = 1;
        foreach (var clubId in clubIds.Take(Math.Min(12, clubIds.Count)))
        {
            for (var k = 0; k < 4; k++)
            {
                var uid = BuildSeedUserGuid(userStart++);
                var key = (clubId, uid);
                if (!used.Add(key))
                    continue;
                memberships.Add(new ClubMembership
                {
                    ClubId = clubId,
                    UserId = uid,
                    CreatedByUserId = uid
                });
            }
        }

        await dbContext.ClubMemberships.AddRangeAsync(memberships, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedEventsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> clubIds,
        CancellationToken cancellationToken)
    {
        if (clubIds.Count == 0 || await dbContext.Events.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var now = DateTime.UtcNow;
        var events = new[]
        {
            ("Founders’ Day Open Campus", "Guided tours, dean’s welcome, and student club fair at ADA University main campus.", "ADA University — Main Atrium", 120),
            ("Baku Tech Talks: Caspian Cloud", "Industry panel with local SaaS founders and coffee networking.", "ADA Learning Hub — Auditorium B", 80),
            ("Nowruz Spring Celebration", "Haft-sin display, traditional music, and student performances.", "ADA Outdoor Plaza", 200),
            ("MUN Crisis Simulation: Energy Security", "Double-delegate crisis committee focused on South Caucasus scenarios.", "ADA — Conference Room 3", 40),
            ("Green ADA: Khizi Tree Planting", "Day trip to foothills; transportation departs 08:00 from ADA parking.", "Khizi Forest (chartered bus from ADA)", 35),
            ("IEEE Embedded Workshop", "Hands-on STM32 labs; kits sponsored by partner companies in Baku.", "ADA Maker Lab", 25),
            ("International Food Evening", "Student booths representing home countries at ADA.", "ADA Student Center", 150),
            ("Chess Simul with Guest Master", "Simultaneous exhibition; beginners welcome on boards 21–30.", "ADA Library Reading Room", 32),
            ("Film Night: Azerbaijani New Wave", "Screening + Q&A with local director; subtitles in English.", "ADA Cinema Corner", 60),
            ("Startup Pitch Night — Fall", "Five-minute pitches; judges from ADA faculty and accelerators.", "ADA Innovation Studio", 70),
            ("Architecture Walk: Icherisheher", "Measured drawings and photo ethics briefing before Old City walk.", "Meet at ADA main gate", 22),
            ("Volunteer Day: Children’s Library", "Reading buddies in Nasimi district; Azerbaijani or English.", "Nasimi Public Library (ADA shuttle)", 18)
        };

        var rows = events
            .Select((e, i) => new Event
            {
                ClubId = clubIds[i % clubIds.Count],
                Name = e.Item1,
                Description = e.Item2,
                StartTime = now.AddDays(7 + i * 3).AddHours(10),
                EndTime = now.AddDays(7 + i * 3).AddHours(12 + (i % 3)),
                Location = e.Item3,
                SeatLimit = e.Item4,
                Requirements = "Valid ADA student ID; register via ADA portal.",
                Prerequisites = i % 4 == 0 ? "None" : "Prior attendance at one club orientation session.",
                Notes = "Seeded for ADA University club management demo.",
                ApprovedBy = BuildSeedUserGuid((i % 20) + 1),
                Status = ClubStatus.Approved,
                CreatedByUserId = BuildSeedUserGuid((i % 20) + 1)
            })
            .ToList();

        await dbContext.Events.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedSubeventsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> eventIds,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0 || await dbContext.Subevents.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var now = DateTime.UtcNow;
        var subevents = new List<Subevent>();
        for (var i = 0; i < Math.Min(12, eventIds.Count * 2); i++)
        {
            var mainId = eventIds[i % eventIds.Count];
            subevents.Add(new Subevent
            {
                MainEventId = mainId,
                Name = i % 2 == 0 ? "Registration & badge pickup" : "Breakout session",
                StartTime = now.AddDays(10 + i).AddHours(9),
                EndTime = now.AddDays(10 + i).AddHours(10),
                CreatedByUserId = BuildSeedUserGuid(300 + i)
            });
        }

        await dbContext.Subevents.AddRangeAsync(subevents, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedEventRegistrationsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> eventIds,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0 || await dbContext.EventRegistrations.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var events = await dbContext.Events
            .Where(e => eventIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken)
            .ConfigureAwait(false);

        var registrations = new List<EventRegistration>();
        var userBase = 400;
        foreach (var eventId in eventIds.Take(8))
        {
            if (!events.TryGetValue(eventId, out var evt))
                continue;

            for (var u = 0; u < 3; u++)
            {
                var userId = BuildSeedUserGuid(userBase++);
                registrations.Add(new EventRegistration
                {
                    EventId = eventId,
                    UserId = userId,
                    TicketId = $"TCK-SEED-{eventId:D4}-{u:D2}",
                    RegistrationJwt = "seed",
                    EventNameSnapshot = evt.Name,
                    EventImageUrlSnapshot = evt.ImageUrl,
                    EventStartTimeSnapshot = evt.StartTime,
                    EventEndTimeSnapshot = evt.EndTime,
                    EventLocationSnapshot = evt.Location,
                    EventSeatLimitSnapshot = evt.SeatLimit,
                    CreatedByUserId = userId
                });
            }
        }

        await dbContext.EventRegistrations.AddRangeAsync(registrations, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedJobVacanciesAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> clubIds,
        IReadOnlyList<int> positionIds,
        CancellationToken cancellationToken)
    {
        if (clubIds.Count == 0 || await dbContext.JobVacancies.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var deadline = DateTime.UtcNow.AddMonths(2).Date;
        var vacancies = new List<JobVacancy>();

        var specs = new (string Title, string Description, bool LinkPosition, bool Draft)[]
        {
            ("Design Lead — club branding", "Lead visual identity for ADA Entrepreneurship Club pitch nights and social templates. Minimum 100 characters: collaborate with founders on decks, maintain Figma libraries, and ensure accessibility contrast for bilingual AZ/EN materials.", true, false),
            ("Treasurer (IEEE)", "Track IEEE branch dues, receipts in AZN, and reimbursement forms through ADA finance office procedures.", true, false),
            ("Social media intern", "Short-form video from campus events; knowledge of Instagram/TikTok trends in Azerbaijan.", false, false),
            ("MUN research analyst", "Write background guides for crisis committees; cite UN and regional sources.", true, false),
            ("Volunteer photographer", "Weekend coverage for Green ADA and sports; release forms required.", false, true),
            ("Hackathon logistics lead", "Sponsor swag, catering, and judge schedules for ADA CS Society spring hackathon.", true, false),
            ("Peer tutor — calculus", "Hold drop-in hours for MATH151 in ADA library; training provided.", false, false),
            ("Events DJ assistant", "Assist with soundchecks for ADA Music showcases; lifting equipment up to 15 kg.", false, true),
            ("Corporate relations officer", "Cold outreach to Baku tech firms for IEEE guest lectures.", true, false),
            ("Content editor (Azerbaijani)", "Proofread AZ copy for International Students Association newsletters.", false, false),
            ("Safety marshal — intramurals", "First-aid basics; enforce ADA gym rules during matches.", true, false),
            ("Alumni liaison", "Coordinate LinkedIn lives with ADA graduates in London and Istanbul hubs.", false, false)
        };

        for (var i = 0; i < specs.Length; i++)
        {
            var clubId = clubIds[i % clubIds.Count];
            int? posId = null;
            if (specs[i].LinkPosition && positionIds.Count > 0)
                posId = positionIds[i % positionIds.Count];

            vacancies.Add(new JobVacancy
            {
                ClubId = clubId,
                Title = specs[i].LinkPosition ? string.Empty : specs[i].Title,
                Description = specs[i].Description,
                PostedBy = BuildSeedUserGuid(500 + i),
                IsActive = !specs[i].Draft,
                IsDraft = specs[i].Draft,
                ClubPositionId = posId,
                ApplicationDeadline = specs[i].Draft ? null : deadline.AddDays(i),
                CreatedByUserId = BuildSeedUserGuid(500 + i)
            });
        }

        await dbContext.JobVacancies.AddRangeAsync(vacancies, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedJobApplicationsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyList<int> vacancyIds,
        CancellationToken cancellationToken)
    {
        if (vacancyIds.Count == 0 || await dbContext.JobApplications.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var applications = new List<JobApplication>();
        var applicantBase = 600;
        for (var i = 0; i < Math.Min(14, vacancyIds.Count * 2); i++)
        {
            applications.Add(new JobApplication
            {
                JobVacancyId = vacancyIds[i % vacancyIds.Count],
                ApplicantId = BuildSeedUserGuid(applicantBase + i),
                ExperienceLinks = $"https://portfolio.ada.edu.az/seed-user-{applicantBase + i}",
                FileUrl = $"https://files.ada.edu.az/cv/seed-{applicantBase + i}.pdf",
                Status = (JobApplicationStatus)(i % 7),
                CreatedByUserId = BuildSeedUserGuid(applicantBase + i)
            });
        }

        await dbContext.JobApplications.AddRangeAsync(applications, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Guid BuildSeedUserGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}
