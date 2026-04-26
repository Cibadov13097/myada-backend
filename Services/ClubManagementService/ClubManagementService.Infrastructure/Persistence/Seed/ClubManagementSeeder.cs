using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Seed;

/// <summary>Development/demo seed data themed for ADA University (Baku, Azerbaijan).</summary>
public static class ClubManagementSeeder
{
    private static readonly SeedClubDefinition[] ClubSeeds =
    {
        new(
            "Tech Innovators Club",
            "Software, startups, hackathons",
            "STEM & Computing",
            new[]
            {
                new SeedEmployeeDefinition("Rauf Məmmədov", "President"),
                new SeedEmployeeDefinition("Nihat Əliyev", "Vice President"),
                new SeedEmployeeDefinition("Orxan Hüseynov", "Tech Lead"),
                new SeedEmployeeDefinition("Elvin Quliyev", "Marketing Lead"),
                new SeedEmployeeDefinition("Kamran Abbasov", "Event Manager")
            },
            new[]
            {
                "Aysel Əliyeva",
                "Nigar Həsənova",
                "Fidan Quliyeva",
                "Ləman Məmmədova",
                "Günay Əliyeva",
                "Tural İbrahimov",
                "Murad Əliyev",
                "Samir Həsənov",
                "Vüqar Rzayev",
                "Elşən Məmmədov"
            }),
        new(
            "Business & Entrepreneurship Club",
            "Startups, leadership, networking",
            "Entrepreneurship",
            new[]
            {
                new SeedEmployeeDefinition("Emil Qasımov", "President"),
                new SeedEmployeeDefinition("Tural Əliyev", "Vice President"),
                new SeedEmployeeDefinition("Rəşad Qurbanov", "Finance Lead"),
                new SeedEmployeeDefinition("Nicat Hüseynov", "Marketing Lead"),
                new SeedEmployeeDefinition("Zaur Məmmədov", "Event Manager")
            },
            new[]
            {
                "Nərmin Həsənova",
                "Şəbnəm Quliyeva",
                "Səidə Əliyeva",
                "İlkin Məmmədli",
                "Kənan Əliyev",
                "Elnur Abbasov",
                "Emin Əliyev",
                "Vüsal Quliyev",
                "Cavid Hüseynov",
                "Anar Məmmədov"
            }),
        new(
            "AI & Data Science Club",
            "Machine learning, analytics",
            "STEM & Computing",
            new[]
            {
                new SeedEmployeeDefinition("Elvin Məmmədli", "President"),
                new SeedEmployeeDefinition("Murad Rzayev", "Vice President"),
                new SeedEmployeeDefinition("Kamal Əliyev", "Tech Lead"),
                new SeedEmployeeDefinition("Fərid Quliyev", "Data Lead"),
                new SeedEmployeeDefinition("Ramin Hüseynov", "Event Manager")
            },
            new[]
            {
                "Aytac Əliyeva",
                "Ləman Qasımova",
                "Günel Məmmədova",
                "Səbinə Əliyeva",
                "Nərgiz Hüseynova",
                "Rauf Əliyev",
                "Samir Qasımov",
                "Tural Hüseynov",
                "Vüqar Məmmədov",
                "Elşad Əliyev"
            }),
        new(
            "Cyber Security Club",
            "Ethical hacking, security",
            "STEM & Computing",
            new[]
            {
                new SeedEmployeeDefinition("Nurlan Əliyev", "President"),
                new SeedEmployeeDefinition("Elnur Quliyev", "Vice President"),
                new SeedEmployeeDefinition("Samir Hüseynov", "Security Lead"),
                new SeedEmployeeDefinition("Orxan Məmmədov", "Tech Lead"),
                new SeedEmployeeDefinition("Kənan Rzayev", "Event Manager")
            },
            new[]
            {
                "Günay Quliyeva",
                "Aysu Məmmədova",
                "Nigar Əliyeva",
                "Ləman Hüseynova",
                "Fidan Əliyeva",
                "Rəşad Hüseynov",
                "Zaur Quliyev",
                "Nicat Məmmədov",
                "Emin Hüseynov",
                "Tural Rzayev"
            }),
        new(
            "Debate & Public Speaking Club",
            "Communication, debates",
            "Media & Communications",
            new[]
            {
                new SeedEmployeeDefinition("Rəşad Əliyev", "President"),
                new SeedEmployeeDefinition("Vüsal Məmmədov", "Vice President"),
                new SeedEmployeeDefinition("Anar Hüseynov", "Content Lead"),
                new SeedEmployeeDefinition("Cavid Quliyev", "Marketing Lead"),
                new SeedEmployeeDefinition("Elşən Əliyev", "Event Manager")
            },
            new[]
            {
                "Nərmin Əliyeva",
                "Şəbnəm Məmmədova",
                "Səbinə Hüseynova",
                "Günel Əliyeva",
                "Ləman Quliyeva",
                "İlkin Hüseynov",
                "Kamran Əliyev",
                "Murad Quliyev",
                "Samir Məmmədov",
                "Vüqar Əliyev"
            })
    };

    private static readonly IReadOnlyDictionary<string, Guid> ClubSeedUserIds = BuildClubSeedUserIds();

    public static async Task SeedAsync(ClubManagementDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(dbContext, cancellationToken).ConfigureAwait(false);

        var categoryIdsByName = BuildCategoryIdsByName(
            await dbContext.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false));

        await SeedClubsAsync(dbContext, categoryIdsByName, cancellationToken).ConfigureAwait(false);

        var seededClubNames = ClubSeeds.Select(x => x.Name).ToArray();
        var seededClubsByName = BuildClubsByName(
            await dbContext.Clubs
                .Where(x => seededClubNames.Contains(x.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false));

        await SeedClubPositionsAsync(dbContext, seededClubsByName, cancellationToken).ConfigureAwait(false);
        await SeedClubEmployeesAsync(dbContext, seededClubsByName, cancellationToken).ConfigureAwait(false);
        await SeedClubMembershipsAsync(dbContext, seededClubsByName, cancellationToken).ConfigureAwait(false);

        var clubIds = await dbContext.Clubs
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var positionIds = await dbContext.ClubPositions
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await SeedEventsAsync(dbContext, clubIds, cancellationToken).ConfigureAwait(false);

        var eventIds = await dbContext.Events
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await SeedSubeventsAsync(dbContext, eventIds, cancellationToken).ConfigureAwait(false);
        await SeedEventRegistrationsAsync(dbContext, eventIds, cancellationToken).ConfigureAwait(false);
        await SeedJobVacanciesAsync(dbContext, clubIds, positionIds, cancellationToken).ConfigureAwait(false);

        var vacancyIds = await dbContext.JobVacancies
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await SeedJobApplicationsAsync(dbContext, vacancyIds, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedCategoriesAsync(ClubManagementDbContext dbContext, CancellationToken cancellationToken)
    {
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

        var existingNames = await dbContext.Categories
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var categories = categoryNames
            .Where(name => !existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Select((name, i) => new Category
            {
                Name = name,
                CreatedByUserId = BuildSeedUserGuid((i % 10) + 1)
            })
            .ToList();

        if (categories.Count == 0)
            return;

        await dbContext.Categories.AddRangeAsync(categories, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyDictionary<string, int> BuildCategoryIdsByName(IEnumerable<Category> categories)
    {
        return categories
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.Id).First().Id,
                StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, Club> BuildClubsByName(IEnumerable<Club> clubs)
    {
        return clubs
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.Id).First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task SeedClubsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyDictionary<string, int> categoryIdsByName,
        CancellationToken cancellationToken)
    {
        if (categoryIdsByName.Count == 0)
            return;

        var seededClubNames = ClubSeeds.Select(x => x.Name).ToArray();
        var existingClubsByName = BuildClubsByName(
            await dbContext.Clubs
                .Where(x => seededClubNames.Contains(x.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false));

        var saveChanges = false;
        foreach (var seed in ClubSeeds)
        {
            var presidentUserId = GetClubSeedUserId(seed.Employees[0].FullName);
            if (!categoryIdsByName.TryGetValue(seed.CategoryName, out var categoryId))
                categoryId = categoryIdsByName.Values.First();

            if (existingClubsByName.TryGetValue(seed.Name, out var existing))
            {
                var changed = false;
                if (!string.Equals(existing.Description, seed.Description, StringComparison.Ordinal))
                {
                    existing.Description = seed.Description;
                    changed = true;
                }

                if (existing.CategoryId != categoryId)
                {
                    existing.CategoryId = categoryId;
                    changed = true;
                }

                if (existing.Status != ClubStatus.Approved)
                {
                    existing.Status = ClubStatus.Approved;
                    changed = true;
                }

                if (existing.ApprovedBy != presidentUserId)
                {
                    existing.ApprovedBy = presidentUserId;
                    changed = true;
                }

                if (existing.CreatedByUserId is null)
                {
                    existing.CreatedByUserId = presidentUserId;
                    changed = true;
                }

                if (changed)
                {
                    existing.ModifiedAt = DateTime.UtcNow;
                    existing.ModifiedByUserId = presidentUserId;
                    saveChanges = true;
                }

                continue;
            }

            await dbContext.Clubs.AddAsync(new Club
            {
                Name = seed.Name,
                Description = seed.Description,
                ApprovedBy = presidentUserId,
                Status = ClubStatus.Approved,
                CategoryId = categoryId,
                CreatedByUserId = presidentUserId
            }, cancellationToken).ConfigureAwait(false);

            saveChanges = true;
        }

        if (!saveChanges)
            return;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubPositionsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyDictionary<string, Club> clubsByName,
        CancellationToken cancellationToken)
    {
        if (clubsByName.Count == 0)
            return;

        var clubIds = clubsByName.Values.Select(x => x.Id).Distinct().ToArray();
        var existingPositionRows = await dbContext.ClubPositions
            .AsNoTracking()
            .Where(x => clubIds.Contains(x.ClubId))
            .Select(x => new { x.ClubId, x.Title })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingKeys = new HashSet<string>(
            existingPositionRows.Select(x => BuildClubPositionKey(x.ClubId, x.Title)),
            StringComparer.Ordinal);

        var positions = new List<ClubPosition>();
        foreach (var seed in ClubSeeds)
        {
            if (!clubsByName.TryGetValue(seed.Name, out var club))
                continue;

            var createdByUserId = GetClubSeedUserId(seed.Employees[0].FullName);
            foreach (var title in seed.Employees
                         .Select(x => x.PositionTitle)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var key = BuildClubPositionKey(club.Id, title);
                if (!existingKeys.Add(key))
                    continue;

                var position = new ClubPosition
                {
                    ClubId = club.Id,
                    Title = title,
                    Category = MapPositionCategory(title),
                    CreatedByUserId = createdByUserId
                };

                var requirements = BuildPositionRequirements(title);
                for (var i = 0; i < requirements.Length; i++)
                {
                    position.Requirements.Add(new ClubPositionRequirement
                    {
                        SortOrder = i,
                        Text = requirements[i]
                    });
                }

                positions.Add(position);
            }
        }

        if (positions.Count == 0)
            return;

        await dbContext.ClubPositions.AddRangeAsync(positions, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubEmployeesAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyDictionary<string, Club> clubsByName,
        CancellationToken cancellationToken)
    {
        if (clubsByName.Count == 0)
            return;

        var clubIds = clubsByName.Values.Select(x => x.Id).Distinct().ToArray();
        var positions = await dbContext.ClubPositions
            .AsNoTracking()
            .Where(x => clubIds.Contains(x.ClubId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var positionIdsByKey = positions
            .GroupBy(x => BuildClubPositionKey(x.ClubId, x.Title), StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).First().Id, StringComparer.Ordinal);

        var existingEmployees = await dbContext.ClubEmployees
            .Where(x => clubIds.Contains(x.ClubId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var employeesByKey = existingEmployees.ToDictionary(x => (x.ClubId, x.UserId));
        var saveChanges = false;

        foreach (var seed in ClubSeeds)
        {
            if (!clubsByName.TryGetValue(seed.Name, out var club))
                continue;

            var presidentUserId = GetClubSeedUserId(seed.Employees[0].FullName);
            foreach (var employee in seed.Employees)
            {
                var userId = GetClubSeedUserId(employee.FullName);
                var positionId = positionIdsByKey[BuildClubPositionKey(club.Id, employee.PositionTitle)];

                if (employeesByKey.TryGetValue((club.Id, userId), out var existing))
                {
                    if (existing.PositionId == positionId)
                        continue;

                    existing.PositionId = positionId;
                    existing.ModifiedAt = DateTime.UtcNow;
                    existing.ModifiedByUserId = presidentUserId;
                    saveChanges = true;
                    continue;
                }

                var row = new ClubEmployee
                {
                    ClubId = club.Id,
                    UserId = userId,
                    PositionId = positionId,
                    CreatedByUserId = presidentUserId
                };

                await dbContext.ClubEmployees.AddAsync(row, cancellationToken).ConfigureAwait(false);
                employeesByKey[(club.Id, userId)] = row;
                saveChanges = true;
            }
        }

        if (!saveChanges)
            return;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedClubMembershipsAsync(
        ClubManagementDbContext dbContext,
        IReadOnlyDictionary<string, Club> clubsByName,
        CancellationToken cancellationToken)
    {
        if (clubsByName.Count == 0)
            return;

        var clubIds = clubsByName.Values.Select(x => x.Id).Distinct().ToArray();
        var existingMemberships = await dbContext.ClubMemberships
            .IgnoreQueryFilters()
            .Where(x => clubIds.Contains(x.ClubId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var membershipsByKey = existingMemberships
            .GroupBy(x => (x.ClubId, x.UserId))
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.Id).First());

        var saveChanges = false;
        foreach (var seed in ClubSeeds)
        {
            if (!clubsByName.TryGetValue(seed.Name, out var club))
                continue;

            foreach (var memberName in seed.Members)
            {
                var userId = GetClubSeedUserId(memberName);
                var key = (club.Id, userId);

                if (membershipsByKey.TryGetValue(key, out var existingMembership))
                {
                    if (!existingMembership.IsDeleted)
                        continue;

                    existingMembership.IsDeleted = false;
                    existingMembership.DeletedAt = null;
                    existingMembership.DeletedByUserId = null;
                    existingMembership.ModifiedAt = DateTime.UtcNow;
                    existingMembership.ModifiedByUserId = userId;
                    existingMembership.CreatedByUserId ??= userId;
                    saveChanges = true;
                    continue;
                }

                var membership = new ClubMembership
                {
                    ClubId = club.Id,
                    UserId = userId,
                    CreatedByUserId = userId
                };

                await dbContext.ClubMemberships.AddAsync(membership, cancellationToken).ConfigureAwait(false);
                membershipsByKey[key] = membership;
                saveChanges = true;
            }
        }

        if (!saveChanges)
            return;

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
            ("Founders Day Open Campus", "Guided tours, dean's welcome, and student club fair at ADA University main campus.", "ADA University - Main Atrium", 120),
            ("Baku Tech Talks: Caspian Cloud", "Industry panel with local SaaS founders and coffee networking.", "ADA Learning Hub - Auditorium B", 80),
            ("Nowruz Spring Celebration", "Haft-sin display, traditional music, and student performances.", "ADA Outdoor Plaza", 200),
            ("MUN Crisis Simulation: Energy Security", "Double-delegate crisis committee focused on South Caucasus scenarios.", "ADA - Conference Room 3", 40),
            ("Green ADA: Khizi Tree Planting", "Day trip to foothills; transportation departs 08:00 from ADA parking.", "Khizi Forest (chartered bus from ADA)", 35),
            ("IEEE Embedded Workshop", "Hands-on STM32 labs; kits sponsored by partner companies in Baku.", "ADA Maker Lab", 25),
            ("International Food Evening", "Student booths representing home countries at ADA.", "ADA Student Center", 150),
            ("Chess Simul with Guest Master", "Simultaneous exhibition; beginners welcome on boards 21-30.", "ADA Library Reading Room", 32),
            ("Film Night: Azerbaijani New Wave", "Screening + Q&A with local director; subtitles in English.", "ADA Cinema Corner", 60),
            ("Startup Pitch Night - Fall", "Five-minute pitches; judges from ADA faculty and accelerators.", "ADA Innovation Studio", 70),
            ("Architecture Walk: Icherisheher", "Measured drawings and photo ethics briefing before Old City walk.", "Meet at ADA main gate", 22),
            ("Volunteer Day: Children's Library", "Reading buddies in Nasimi district; Azerbaijani or English.", "Nasimi Public Library (ADA shuttle)", 18)
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
            ("Design Lead - club branding", "Lead visual identity for ADA Entrepreneurship Club pitch nights and social templates. Minimum 100 characters: collaborate with founders on decks, maintain Figma libraries, and ensure accessibility contrast for bilingual AZ/EN materials.", true, false),
            ("Treasurer (IEEE)", "Track IEEE branch dues, receipts in AZN, and reimbursement forms through ADA finance office procedures.", true, false),
            ("Social media intern", "Short-form video from campus events; knowledge of Instagram/TikTok trends in Azerbaijan.", false, false),
            ("MUN research analyst", "Write background guides for crisis committees; cite UN and regional sources.", true, false),
            ("Volunteer photographer", "Weekend coverage for Green ADA and sports; release forms required.", false, true),
            ("Hackathon logistics lead", "Sponsor swag, catering, and judge schedules for ADA CS Society spring hackathon.", true, false),
            ("Peer tutor - calculus", "Hold drop-in hours for MATH151 in ADA library; training provided.", false, false),
            ("Events DJ assistant", "Assist with soundchecks for ADA Music showcases; lifting equipment up to 15 kg.", false, true),
            ("Corporate relations officer", "Cold outreach to Baku tech firms for IEEE guest lectures.", true, false),
            ("Content editor (Azerbaijani)", "Proofread AZ copy for International Students Association newsletters.", false, false),
            ("Safety marshal - intramurals", "First-aid basics; enforce ADA gym rules during matches.", true, false),
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

    private static Guid GetClubSeedUserId(string fullName)
    {
        if (ClubSeedUserIds.TryGetValue(fullName, out var userId))
            return userId;

        throw new InvalidOperationException($"No deterministic seed user id is configured for '{fullName}'.");
    }

    private static IReadOnlyDictionary<string, Guid> BuildClubSeedUserIds()
    {
        var result = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var nextIndex = 1000;

        foreach (var name in ClubSeeds
                     .SelectMany(x => x.Employees.Select(e => e.FullName).Concat(x.Members))
                     .Distinct(StringComparer.Ordinal))
        {
            result[name] = BuildSeedUserGuid(nextIndex++);
        }

        return result;
    }

    private static string BuildClubPositionKey(int clubId, string title)
    {
        return $"{clubId}:{title.Trim().ToUpperInvariant()}";
    }

    private static ClubPositionCategory MapPositionCategory(string title)
    {
        return title.Trim() switch
        {
            "Tech Lead" => ClubPositionCategory.Technology,
            "Data Lead" => ClubPositionCategory.Technology,
            "Security Lead" => ClubPositionCategory.Technology,
            "Marketing Lead" => ClubPositionCategory.Marketing,
            "Event Manager" => ClubPositionCategory.Events,
            "Finance Lead" => ClubPositionCategory.Finance,
            "Content Lead" => ClubPositionCategory.Content,
            _ => ClubPositionCategory.Other
        };
    }

    private static string[] BuildPositionRequirements(string title)
    {
        return title.Trim() switch
        {
            "President" => new[]
            {
                "Lead the club strategy, represent the team in official meetings, and keep semester goals on track.",
                "Coordinate officers, delegate responsibilities clearly, and review weekly progress with the team."
            },
            "Vice President" => new[]
            {
                "Support the president in day-to-day coordination and step in when leadership coverage is needed.",
                "Help align officer tasks, follow up on deadlines, and keep internal operations moving smoothly."
            },
            "Tech Lead" => new[]
            {
                "Guide technical sessions, project work, and hands-on learning activities for club members.",
                "Mentor contributors, review implementation plans, and help the club ship demos with quality."
            },
            "Data Lead" => new[]
            {
                "Support data workflows, analytics tasks, and project organization for club initiatives.",
                "Help members work with datasets responsibly and turn findings into clear outputs."
            },
            "Security Lead" => new[]
            {
                "Lead safe cybersecurity practice sessions and promote ethical handling of tools and exercises.",
                "Help members understand secure workflows, responsible disclosure, and defensive thinking."
            },
            "Marketing Lead" => new[]
            {
                "Plan club promotion, publish event messaging, and maintain a consistent public voice.",
                "Coordinate campaign content, social posts, and outreach materials before major activities."
            },
            "Finance Lead" => new[]
            {
                "Track budgets, event expenses, and sponsorship-related planning with reliable documentation.",
                "Prepare simple cost estimates and help the club make practical spending decisions."
            },
            "Content Lead" => new[]
            {
                "Prepare educational material, session outlines, and speaking content for club programs.",
                "Keep content clear, structured, and aligned with the club's learning goals."
            },
            "Event Manager" => new[]
            {
                "Own event logistics, timelines, and execution details from planning through wrap-up.",
                "Coordinate rooms, checklists, and day-of operations so events run smoothly."
            },
            _ => new[]
            {
                "Contribute consistently to club planning, communication, and execution in the assigned role.",
                "Collaborate with the leadership team and follow through on responsibilities before deadlines."
            }
        };
    }

    private sealed record SeedClubDefinition(
        string Name,
        string Description,
        string CategoryName,
        IReadOnlyList<SeedEmployeeDefinition> Employees,
        IReadOnlyList<string> Members);

    private sealed record SeedEmployeeDefinition(string FullName, string PositionTitle);
}
