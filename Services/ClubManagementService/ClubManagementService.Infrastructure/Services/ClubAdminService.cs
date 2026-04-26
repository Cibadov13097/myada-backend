using System.Collections.Generic;
using System.Globalization;
using ClubManagementService.Application.ClubAdmin;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ClubManagementService.Infrastructure.Persistence.Context;

namespace ClubManagementService.Infrastructure.Services;

public sealed class ClubAdminService : IClubAdminService
{
    private sealed record ClubRouteContext(int ClubId, string Key);
    private readonly IClubService _clubService;
    private readonly IJobVacancyService _jobVacancyService;
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IEventService _eventService;
    private readonly IClubPositionRepository _positionRepository;
    private readonly ClubAdminWorkspaceStore _workspace;
    private readonly IProposalContractService _proposalContract;
    private readonly IClubRepository _clubRepository;
    private readonly IClubMembershipRepository _clubMembershipRepository;
    private readonly ClubManagementDbContext _db;
    private readonly IAuthUserLookupService _authUserLookup;
    private readonly ILogger<ClubAdminService> _logger;

    public ClubAdminService(
        IClubService clubService,
        IJobVacancyService jobVacancyService,
        IJobApplicationService jobApplicationService,
        IEventService eventService,
        IClubPositionRepository positionRepository,
        ClubAdminWorkspaceStore workspace,
        IProposalContractService proposalContract,
        IClubRepository clubRepository,
        IClubMembershipRepository clubMembershipRepository,
        ClubManagementDbContext db,
        IAuthUserLookupService authUserLookup,
        ILogger<ClubAdminService> logger)
    {
        _clubService = clubService;
        _jobVacancyService = jobVacancyService;
        _jobApplicationService = jobApplicationService;
        _eventService = eventService;
        _positionRepository = positionRepository;
        _workspace = workspace;
        _proposalContract = proposalContract;
        _clubRepository = clubRepository;
        _clubMembershipRepository = clubMembershipRepository;
        _db = db;
        _authUserLookup = authUserLookup;
        _logger = logger;
    }

    public async Task<ClubAdminDashboardDto?> GetDashboardAsync(string clubRouteKey, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var vacancies = await _jobVacancyService.GetByClubIdAsync(ctx.ClubId).ConfigureAwait(false);
        var openVacancies = vacancies.Count(v => !v.IsDraft && v.IsActive);
        var jobApps = await _jobApplicationService.GetByClubIdAsync(ctx.ClubId).ConfigureAwait(false);
        var newApps = jobApps.Count(a => a.Status is JobApplicationStatus.Pending or JobApplicationStatus.Reviewing or JobApplicationStatus.InterviewInvited);
        newApps += ws.MembershipApplications.Values.Count(m =>
            m.Status is "Pending" or "Reviewing");
        var events = await _eventService.GetByClubIdAsync(ctx.ClubId).ConfigureAwait(false);
        var upcoming = events.Count(e => e.StartTime >= DateTime.UtcNow);
        var members = ws.Members.Count;

        return new ClubAdminDashboardDto
        {
            ClubId = ctx.Key,
            Stats = new ClubAdminDashboardStatsDto
            {
                ActiveMembers = members,
                OpenVacancies = openVacancies,
                NewApplications = newApps,
                UpcomingEvents = upcoming
            },
            Activity = Array.Empty<object>()
        };
    }

    public async Task<ClubAdminPagedResult<ClubAdminApplicationListItemDto>> GetApplicationsAsync(
        string clubRouteKey,
        string? type,
        string? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminApplicationListItemDto>(page, limit);

        var clubId = ctx.ClubId;
        var ws = _workspace.GetOrCreate(clubId);
        var vacancies = await _jobVacancyService.GetByClubIdAsync(clubId).ConfigureAwait(false);
        var vacancyById = vacancies.ToDictionary(v => v.Id, v => v.Title);

        var list = new List<ClubAdminApplicationListItemDto>();
        var typeNorm = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLowerInvariant();

        if (typeNorm is "all" or "job")
        {
            var jobApps = await _jobApplicationService.GetByClubIdAsync(clubId).ConfigureAwait(false);
            foreach (var a in jobApps.Where(a => ClubAdminStatusMaps.JobMatchesApiFilter(status, a.Status)))
            {
                vacancyById.TryGetValue(a.JobVacancyId, out var title);
                list.Add(new ClubAdminApplicationListItemDto
                {
                    Id = ClubAdminApplicationId.Job(a.Id),
                    ApplicationType = "job",
                    Status = ClubAdminStatusMaps.JobApplicationToApi(a.Status),
                    Job = new ClubAdminJobApplicationBriefDto
                    {
                        VacancyId = a.JobVacancyId.ToString(CultureInfo.InvariantCulture),
                        ApplicantUserId = a.ApplicantId.ToString("D"),
                        VacancyTitle = title
                    }
                });
            }
        }

        if (typeNorm is "all" or "membership")
        {
            foreach (var m in ws.MembershipApplications.Values
                         .Where(m =>
                             // Default list hides decided applications (Approved/Rejected)
                             (string.IsNullOrWhiteSpace(status)
                                 ? !string.Equals(m.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(m.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                                 : ClubAdminStatusMaps.MembershipMatchesApiFilter(status, m.Status))))
            {
                list.Add(new ClubAdminApplicationListItemDto
                {
                    Id = ClubAdminApplicationId.Membership(m.Id),
                    ApplicationType = "membership",
                    Status = ClubAdminStatusMaps.NormalizeMembershipStatus(m.Status),
                    Membership = new ClubAdminMembershipApplicationBriefDto
                    {
                        ApplicantUserId = m.ApplicantUserId,
                        LetterOfPurpose = m.LetterOfPurpose,
                        Links = m.Links
                    }
                });
            }
        }

        return Page(list.OrderByDescending(x => x.Id), page, limit);
    }

    public async Task<ClubAdminApplicationDetailDto?> GetApplicationAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out var membershipId))
            return null;

        if (jobId > 0)
        {
            var dto = await _jobApplicationService.GetByIdAsync(jobId).ConfigureAwait(false);
            if (dto == null)
                return null;
            var vacancy = await _jobVacancyService.GetByIdAsync(dto.JobVacancyId).ConfigureAwait(false);
            if (vacancy == null || vacancy.ClubId != clubId)
                return null;

            return new ClubAdminApplicationDetailDto
            {
                Id = ClubAdminApplicationId.Job(dto.Id),
                ApplicationType = "job",
                Status = ClubAdminStatusMaps.JobApplicationToApi(dto.Status),
                Job = new ClubAdminJobApplicationDetailDto
                {
                    VacancyId = dto.JobVacancyId.ToString(CultureInfo.InvariantCulture),
                    ApplicantUserId = dto.ApplicantId.ToString("D"),
                    VacancyTitle = vacancy.Title,
                    ExperienceLinks = dto.ExperienceLinks,
                    FileUrl = dto.FileUrl,
                    CreatedAt = dto.CreatedAt.ToUniversalTime().ToString("O"),
                    InterviewSlotId = dto.InterviewSlotId
                },
                Notes = SnapshotNotesForClub(clubId, applicationId)
            };
        }

        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.MembershipApplications.TryGetValue(membershipId, out var row))
            return null;

        return new ClubAdminApplicationDetailDto
        {
            Id = ClubAdminApplicationId.Membership(row.Id),
            ApplicationType = "membership",
            Status = ClubAdminStatusMaps.NormalizeMembershipStatus(row.Status),
            Membership = new ClubAdminMembershipApplicationDetailDto
            {
                ApplicantUserId = row.ApplicantUserId,
                LetterOfPurpose = row.LetterOfPurpose,
                Links = row.Links,
                CreatedAt = row.CreatedAt.ToString("O")
            },
            Notes = SnapshotNotesForClub(clubId, applicationId)
        };
    }

    public async Task<ClubAdminApplicationActionResultDto?> ApproveApplicationAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out var membershipId))
            return null;

        if (jobId > 0)
        {
            if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
                return null;
            await _jobApplicationService.UpdateStatusAsync(jobId, new UpdateJobApplicationStatusDto
                { Status = JobApplicationStatus.Approved }).ConfigureAwait(false);
            return new ClubAdminApplicationActionResultDto
                { ApplicationId = applicationId, Status = "Approved" };
        }

        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.MembershipApplications.TryGetValue(membershipId, out var row))
            return null;
        if (!Guid.TryParse(row.ApplicantUserId, out var applicantGuid))
            throw new ArgumentException("Invalid applicantUserId for membership application.");

        // Add user to real membership table (idempotent with unique index on (ClubId, UserId))
        try
        {
            await _clubMembershipRepository.AddAsync(new ClubMembership
            {
                ClubId = clubId,
                UserId = applicantGuid
            }).ConfigureAwait(false);
            await _clubMembershipRepository.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // If the membership already exists, treat approve as idempotent.
        }

        row.Status = "Approved";
        return new ClubAdminApplicationActionResultDto { ApplicationId = applicationId, Status = "Approved" };
    }

    public async Task<ClubAdminApplicationActionResultDto?> RejectApplicationAsync(string clubRouteKey, string applicationId, string? reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out var membershipId))
            return null;

        if (jobId > 0)
        {
            if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
                return null;
            await _jobApplicationService.UpdateStatusAsync(jobId, new UpdateJobApplicationStatusDto
                { Status = JobApplicationStatus.Rejected }).ConfigureAwait(false);
            return new ClubAdminApplicationActionResultDto
                { ApplicationId = applicationId, Status = "Rejected" };
        }

        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.MembershipApplications.TryGetValue(membershipId, out var row))
            return null;
        row.Status = "Rejected";
        return new ClubAdminApplicationActionResultDto { ApplicationId = applicationId, Status = "Rejected" };
    }

    public async Task<ClubAdminApplicationActionResultDto?> RequestChangesAsync(string clubRouteKey, string applicationId, string? changes, CancellationToken cancellationToken = default)
    {
        _ = changes;
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out var membershipId))
            return null;

        if (jobId > 0)
        {
            if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
                return null;
            await _jobApplicationService.UpdateStatusAsync(jobId, new UpdateJobApplicationStatusDto
                { Status = JobApplicationStatus.Reviewing }).ConfigureAwait(false);
            return new ClubAdminApplicationActionResultDto
                { ApplicationId = applicationId, Status = "Reviewing" };
        }

        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.MembershipApplications.TryGetValue(membershipId, out var row))
            return null;
        row.Status = "Reviewing";
        return new ClubAdminApplicationActionResultDto { ApplicationId = applicationId, Status = "Reviewing" };
    }

    public async Task<ClubAdminApplicationActionResultDto?> InviteToInterviewAsync(string clubRouteKey, string applicationId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out _) || jobId <= 0)
            return null;

        if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
            return null;

        var dto = await _jobApplicationService.GetByIdAsync(jobId).ConfigureAwait(false);
        if (dto == null)
            return null;

        if (dto.Status == JobApplicationStatus.InterviewScheduled)
            return null;

        await _jobApplicationService
            .UpdateStatusAsync(jobId, new UpdateJobApplicationStatusDto { Status = JobApplicationStatus.InterviewInvited })
            .ConfigureAwait(false);

        return new ClubAdminApplicationActionResultDto
        {
            ApplicationId = applicationId,
            Status = ClubAdminStatusMaps.JobApplicationToApi(JobApplicationStatus.InterviewInvited)
        };
    }

    public async Task<ClubAdminApplicationNoteDto?> AddApplicationNoteAsync(string clubRouteKey, string applicationId, string note, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!ClubAdminApplicationId.TryParse(applicationId, out var jobId, out var membershipId))
            return null;

        if (jobId > 0)
        {
            if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
                return null;
        }
        else
        {
            var ws = _workspace.GetOrCreate(clubId);
            if (!ws.MembershipApplications.ContainsKey(membershipId))
                return null;
        }

        var wsNotes = _workspace.GetOrCreate(clubId);
        var q = wsNotes.ApplicationNotes.GetOrAdd(applicationId, _ => new System.Collections.Concurrent.ConcurrentQueue<ApplicationNoteRow>());
        var row = new ApplicationNoteRow { Text = note, CreatedAt = DateTimeOffset.UtcNow };
        q.Enqueue(row);
        return new ClubAdminApplicationNoteDto { Text = row.Text, CreatedAt = row.CreatedAt.ToString("O") };
    }

    public async Task<BulkDecisionResultDto> BulkDecisionAsync(string clubRouteKey, IReadOnlyList<string> applicationIds, string decision, string? reason, CancellationToken cancellationToken = default)
    {
        _ = reason;
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return new BulkDecisionResultDto { Updated = 0 };

        var clubId = ctx.ClubId;
        var updated = 0;
        foreach (var id in applicationIds)
        {
            if (!ClubAdminApplicationId.TryParse(id, out var jobId, out var membershipId))
                continue;

            if (jobId > 0)
            {
                if (!await JobApplicationBelongsToClubAsync(jobId, clubId).ConfigureAwait(false))
                    continue;
                var status = ClubAdminStatusMaps.ParseBulkJobDecision(decision);
                await _jobApplicationService.UpdateStatusAsync(jobId, new UpdateJobApplicationStatusDto { Status = status })
                    .ConfigureAwait(false);
                updated++;
                continue;
            }

            var membershipDecision = ClubAdminStatusMaps.ParseBulkMembershipDecision(decision);
            if (membershipDecision == null)
                continue;
            var ws = _workspace.GetOrCreate(clubId);
            if (!ws.MembershipApplications.TryGetValue(membershipId, out var row))
                continue;
            row.Status = membershipDecision;
            updated++;
        }

        return new BulkDecisionResultDto { Updated = updated };
    }

    public async Task<ClubAdminPagedResult<ClubAdminInterviewSlotDto>> GetInterviewSlotsAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminInterviewSlotDto>(page, limit);

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var p = Math.Max(page, 1);
        var l = Math.Max(limit, 1);
        var query = _db.InterviewSlots
            .AsNoTracking()
            .Where(s => s.ClubId == clubId)
            .OrderBy(s => s.StartsAt);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .Skip((p - 1) * l)
            .Take(l)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(s => MapInterviewSlotDto(s, key)).ToList();
        return new ClubAdminPagedResult<ClubAdminInterviewSlotDto> { Items = items, Page = p, Limit = l, Total = total };
    }

    public async Task<ClubAdminPagedResult<ClubAdminInterviewSlotDto>> GenerateInterviewSlotsAsync(string clubRouteKey, GenerateInterviewSlotsInput input, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminInterviewSlotDto>(1, 20);

        var clubId = ctx.ClubId;
        var key = ctx.Key;

        int? jobVacancyId = null;
        if (!string.IsNullOrWhiteSpace(input.VacancyId))
        {
            if (!int.TryParse(input.VacancyId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vid))
                return EmptyPage<ClubAdminInterviewSlotDto>(1, 20);

            var vacancy = await _db.JobVacancies
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vid && v.ClubId == clubId, cancellationToken)
                .ConfigureAwait(false);
            if (vacancy == null)
                return EmptyPage<ClubAdminInterviewSlotDto>(1, 20);
            jobVacancyId = vid;
        }

        if (!TryBuildSlotSeries(input, out var slotTimes))
            return EmptyPage<ClubAdminInterviewSlotDto>(1, 20);

        var now = DateTime.UtcNow;
        var created = new List<ClubAdminInterviewSlotDto>();
        foreach (var slot in slotTimes)
        {
            var entity = new InterviewSlot
            {
                SlotId = Guid.NewGuid().ToString("D"),
                ClubId = clubId,
                JobVacancyId = jobVacancyId,
                StartsAt = slot.Start,
                EndsAt = slot.End,
                Capacity = 1,
                IsPublished = true,
                CreatedAt = now,
                IsDeleted = false
            };
            await _db.InterviewSlots.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            created.Add(MapInterviewSlotDto(entity, key));
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var l = Math.Max(created.Count, 1);
        return new ClubAdminPagedResult<ClubAdminInterviewSlotDto> { Items = created, Page = 1, Limit = l, Total = created.Count };
    }

    public async Task<ClubAdminInterviewSlotDto?> PatchInterviewSlotAsync(string clubRouteKey, string slotId, PatchInterviewSlotInput input, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        var row = await _db.InterviewSlots
            .FirstOrDefaultAsync(s => s.SlotId == slotId && s.ClubId == clubId, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return null;

        if (input.StartsAt.HasValue)
            row.StartsAt = input.StartsAt.Value;
        if (input.EndsAt.HasValue)
            row.EndsAt = input.EndsAt.Value;
        if (input.VacancyId != null)
        {
            if (string.IsNullOrWhiteSpace(input.VacancyId))
            {
                row.JobVacancyId = null;
            }
            else if (int.TryParse(input.VacancyId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vid))
            {
                var vacancy = await _db.JobVacancies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == vid && v.ClubId == clubId, cancellationToken)
                    .ConfigureAwait(false);
                if (vacancy == null)
                    return null;
                row.JobVacancyId = vid;
            }
            else
            {
                return null;
            }
        }

        if (input.Capacity.HasValue)
            row.Capacity = Math.Max(1, input.Capacity.Value);
        if (input.IsPublished.HasValue)
            row.IsPublished = input.IsPublished.Value;

        row.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapInterviewSlotDto(row, ctx.Key);
    }

    public async Task<bool> DeleteInterviewSlotAsync(string clubRouteKey, string slotId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        var row = await _db.InterviewSlots
            .FirstOrDefaultAsync(s => s.SlotId == slotId && s.ClubId == ctx.ClubId, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return false;

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static ClubAdminInterviewSlotDto MapInterviewSlotDto(InterviewSlot s, string clubRouteKey) =>
        new()
        {
            Id = s.SlotId,
            ClubId = clubRouteKey,
            StartsAt = s.StartsAt,
            EndsAt = s.EndsAt,
            VacancyId = s.JobVacancyId?.ToString(CultureInfo.InvariantCulture),
            Capacity = s.Capacity,
            IsPublished = s.IsPublished
        };

    public async Task<ClubAdminPagedResult<ClubAdminVacancyAdminDto>> GetVacanciesAsync(string clubRouteKey, string? status, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminVacancyAdminDto>(page, limit);

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var list = await _jobVacancyService.GetByClubIdAsync(clubId).ConfigureAwait(false);
        var filtered = list.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (s is "draft")
                filtered = filtered.Where(v => v.IsDraft);
            else if (s is "active" or "published")
                filtered = filtered.Where(v => !v.IsDraft && v.IsActive);
            else if (s is "inactive")
                filtered = filtered.Where(v => !v.IsDraft && !v.IsActive);
        }

        var mapped = filtered
            .OrderByDescending(v => v.Id)
            .Select(v => MapVacancyAdminDto(v, key))
            .ToList();

        return Page(mapped, page, limit);
    }

    public async Task<ClubAdminVacancyAdminDto> CreateVacancyAsync(string clubRouteKey, CreateJobVacancyDto dto, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            throw new InvalidOperationException("Club not found.");

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        dto.ClubId = clubId;

        if (dto.ClubPositionId is > 0)
        {
            var pos = await _positionRepository
                .GetByIdWithDetailsAsync(dto.ClubPositionId.Value, clubId, cancellationToken)
                .ConfigureAwait(false);
            if (pos == null)
                throw new ArgumentException("The selected position does not exist or does not belong to this club.");
        }

        var created = await _jobVacancyService.CreateAsync(dto).ConfigureAwait(false);
        return MapVacancyAdminDto(created, key);
    }

    public async Task<ClubAdminVacancyAdminDto?> PatchVacancyAsync(string clubRouteKey, string vacancyId, UpdateJobVacancyDto dto, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        if (!int.TryParse(vacancyId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        var existing = await _jobVacancyService.GetByIdAsync(id).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return null;

        if (dto.ClubPositionId is > 0)
        {
            var pos = await _positionRepository
                .GetByIdWithDetailsAsync(dto.ClubPositionId.Value, clubId, cancellationToken)
                .ConfigureAwait(false);
            if (pos == null)
                throw new ArgumentException("The selected position does not exist or does not belong to this club.");
        }

        var updated = await _jobVacancyService.UpdateAsync(id, dto).ConfigureAwait(false);
        return MapVacancyAdminDto(updated, key);
    }

    public async Task<ClubAdminVacancyAdminDto?> PatchVacancyStatusAsync(string clubRouteKey, string vacancyId, string status, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        if (!int.TryParse(vacancyId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        var existing = await _jobVacancyService.GetByIdAsync(id).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return null;

        bool isDraft;
        bool isActive;
        switch (status.Trim().ToLowerInvariant())
        {
            case "draft":
                isDraft = true;
                isActive = false;
                break;
            case "published":
            case "active":
                isDraft = false;
                isActive = true;
                break;
            case "inactive":
                isDraft = false;
                isActive = false;
                break;
            default:
                isDraft = existing.IsDraft;
                isActive = existing.IsActive;
                break;
        }

        var updated = await _jobVacancyService.UpdateAsync(id, new UpdateJobVacancyDto
        {
            Title = existing.Title,
            Description = existing.Description,
            IsActive = isActive,
            IsDraft = isDraft
        }).ConfigureAwait(false);

        return MapVacancyAdminDto(updated, key);
    }

    public async Task<ClubAdminPagedResult<ClubAdminPositionDto>> GetPositionsAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminPositionDto>(page, limit);

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var list = await _positionRepository.ListByClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        var items = list
            .OrderBy(p => p.Title)
            .Select(p => MapPositionDto(p, key))
            .ToList();
        return Page(items, page, limit);
    }

    public async Task<ClubAdminPositionDto?> GetPositionAsync(string clubRouteKey, string positionId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        if (!int.TryParse(positionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            return null;

        var entity = await _positionRepository
            .GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken)
            .ConfigureAwait(false);
        return entity == null ? null : MapPositionDto(entity, ctx.Key);
    }

    public async Task<ClubAdminPositionDto> CreatePositionAsync(string clubRouteKey, ClubPositionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false)
                  ?? throw new InvalidOperationException("Club not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Position title is required.");

        var category = EnsureValidPositionCategory(dto.Category);

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var position = new ClubPosition
        {
            ClubId = clubId,
            Title = dto.Title.Trim(),
            Category = category
        };
        var order = 0;
        foreach (var line in dto.Requirements.Where(x => !string.IsNullOrWhiteSpace(x)))
            position.Requirements.Add(new ClubPositionRequirement { SortOrder = order++, Text = line.Trim() });

        await _positionRepository.AddAsync(position, cancellationToken).ConfigureAwait(false);
        await _positionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var loaded = await _positionRepository
            .GetByIdWithDetailsNoTrackingAsync(position.Id, clubId, cancellationToken)
            .ConfigureAwait(false);
        return MapPositionDto(loaded ?? position, key);
    }

    public async Task<ClubAdminPositionDto?> PatchPositionAsync(string clubRouteKey, string positionId, ClubPositionUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        if (!int.TryParse(positionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            return null;

        var entity = await _positionRepository.GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return null;

        var category = EnsureValidPositionCategory(dto.Category);

        if (!string.IsNullOrWhiteSpace(dto.Title))
            entity.Title = dto.Title.Trim();
        entity.Category = category;
        await _positionRepository.ReplaceRequirementsAsync(entity.Id, dto.Requirements, cancellationToken).ConfigureAwait(false);
        await _positionRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        await _positionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var loaded = await _positionRepository
            .GetByIdWithDetailsNoTrackingAsync(entity.Id, ctx.ClubId, cancellationToken)
            .ConfigureAwait(false);
        return MapPositionDto(loaded ?? entity, ctx.Key);
    }

    public async Task<bool> DeletePositionAsync(string clubRouteKey, string positionId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        if (!int.TryParse(positionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            return false;

        var entity = await _positionRepository.GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return false;

        await _positionRepository.ClearVacancyPositionLinksAsync(pid, cancellationToken).ConfigureAwait(false);
        await _positionRepository.SoftDeleteAsync(entity, cancellationToken).ConfigureAwait(false);
        await _positionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<ClubAdminPagedResult<ClubAdminMemberDto>> GetMembersAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminMemberDto>(page, limit);

        var userIds = await _clubMembershipRepository.GetUserIdsByClubAsync(ctx.ClubId, cancellationToken).ConfigureAwait(false);
        var items = userIds
            .Select(u => new ClubAdminMemberDto
            {
                Id = u.ToString("D"),
                ClubId = ctx.Key,
                UserId = u.ToString("D")
            })
            .ToList();
        return Page(items, page, limit);
    }

    public async Task<bool> DeleteMemberAsync(string clubRouteKey, string memberId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        // List members use user id (GUID) as DTO id; remove the real club membership row.
        if (!Guid.TryParse(memberId, out var userId) || userId == Guid.Empty)
            return false;

        var row = await _db.ClubMemberships
            .FirstOrDefaultAsync(m => m.ClubId == ctx.ClubId && m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return false;

        row.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<ClubAdminPagedResult<ClubAdminEmployeeDto>> GetEmployeesAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminEmployeeDto>(page, limit);

        var rows = await _db.ClubEmployees
            .AsNoTracking()
            .Where(e => e.ClubId == ctx.ClubId)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var positionTitlesById = await _db.ClubPositions
            .AsNoTracking()
            .Where(p => p.ClubId == ctx.ClubId)
            .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken)
            .ConfigureAwait(false);

        var userIds = rows.Select(e => e.UserId).Distinct().ToList();
        var userNames = await TryGetUserNamesAsync(userIds, ctx.Key, cancellationToken).ConfigureAwait(false);

        var items = rows.Select(e => new ClubAdminEmployeeDto
        {
            Id = e.Id.ToString(CultureInfo.InvariantCulture),
            ClubId = ctx.Key,
            StudentId = e.UserId.ToString("D"),
            EmployeeName = userNames.TryGetValue(e.UserId, out var name) ? name : null,
            PositionId = e.PositionId?.ToString(CultureInfo.InvariantCulture),
            PositionTitle = e.PositionId.HasValue && positionTitlesById.TryGetValue(e.PositionId.Value, out var title) ? title : null,
            PositionName = e.PositionId.HasValue && positionTitlesById.TryGetValue(e.PositionId.Value, out var legacyTitle) ? legacyTitle : null
        }).ToList();
        return Page(items, page, limit);
    }

    public async Task<ClubAdminEmployeeDto> CreateEmployeeAsync(string clubRouteKey, string studentId, string positionId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false)
                  ?? throw new InvalidOperationException("Club not found.");

        if (!Guid.TryParse(studentId, out var userId) || userId == Guid.Empty)
            throw new ArgumentException("studentId must be a valid GUID user id.");

        int? pidOrNull = null;
        string? positionTitle = null;
        if (int.TryParse(positionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
        {
            var pos = await _positionRepository
                .GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken)
                .ConfigureAwait(false);
            if (pos == null)
                throw new ArgumentException("Position not found for this club.");
            pidOrNull = pid;
            positionTitle = pos.Title;
        }

        var existing = await _db.ClubEmployees
            .FirstOrDefaultAsync(e => e.ClubId == ctx.ClubId && e.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (existing == null)
        {
            existing = new ClubEmployee { ClubId = ctx.ClubId, UserId = userId, PositionId = pidOrNull };
            _db.ClubEmployees.Add(existing);
        }
        else
        {
            existing.PositionId = pidOrNull;
        }
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var userNames = await TryGetUserNamesAsync(new[] { userId }, ctx.Key, cancellationToken).ConfigureAwait(false);

        return new ClubAdminEmployeeDto
        {
            Id = existing.Id.ToString(CultureInfo.InvariantCulture),
            ClubId = ctx.Key,
            StudentId = existing.UserId.ToString("D"),
            EmployeeName = userNames.TryGetValue(userId, out var name) ? name : null,
            PositionId = existing.PositionId?.ToString(CultureInfo.InvariantCulture),
            PositionTitle = positionTitle,
            PositionName = positionTitle
        };
    }

    public async Task PatchEmployeePositionsAsync(string clubRouteKey, IReadOnlyList<EmployeePositionChange> changes, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return;

        foreach (var c in changes)
        {
            if (!int.TryParse(c.EmployeeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eid) || eid <= 0)
                continue;

            int? pidOrNull = null;
            if (int.TryParse(c.PositionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            {
                var pos = await _positionRepository
                    .GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken)
                    .ConfigureAwait(false);
                if (pos == null)
                    throw new ArgumentException("Position not found for this club.");
                pidOrNull = pid;
            }

            var row = await _db.ClubEmployees
                .FirstOrDefaultAsync(e => e.Id == eid && e.ClubId == ctx.ClubId, cancellationToken)
                .ConfigureAwait(false);
            if (row != null)
                row.PositionId = pidOrNull;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteEmployeeAsync(string clubRouteKey, string employeeId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        if (!int.TryParse(employeeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eid) || eid <= 0)
            return false;

        var row = await _db.ClubEmployees
            .FirstOrDefaultAsync(e => e.Id == eid && e.ClubId == ctx.ClubId, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return false;

        _db.ClubEmployees.Remove(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<ClubAdminPagedResult<ClubAdminEventAdminDto>> GetEventsAsync(string clubRouteKey, string? status, string? search, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminEventAdminDto>(page, limit);

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var events = await _eventService.GetByClubIdAsync(clubId).ConfigureAwait(false);
        var filtered = events.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(e => string.Equals(e.Status.ToString(), status, StringComparison.OrdinalIgnoreCase));

        var mapped = filtered
            .OrderBy(e => e.StartTime)
            .Select(e => new ClubAdminEventAdminDto
            {
                Id = e.Id.ToString(CultureInfo.InvariantCulture),
                ClubId = key,
                Name = e.Name,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location,
                SeatLimit = e.SeatLimit,
                Status = e.Status.ToString()
            })
            .ToList();

        return Page(mapped, page, limit);
    }

    public async Task<ClubAdminEventAdminDto?> PatchEventAsync(string clubRouteKey, string eventId, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        if (!int.TryParse(eventId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        var existing = await _eventService.GetByIdAsync(id).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return null;

        var updated = await _eventService.UpdateAsync(id, dto).ConfigureAwait(false);
        return new ClubAdminEventAdminDto
        {
            Id = updated.Id.ToString(CultureInfo.InvariantCulture),
            ClubId = key,
            Name = updated.Name,
            Description = updated.Description,
            StartTime = updated.StartTime,
            EndTime = updated.EndTime,
            Location = updated.Location,
            SeatLimit = updated.SeatLimit,
            Status = updated.Status.ToString()
        };
    }

    public async Task<EventProposalResultDto?> CreateEventProposalAsync(string clubRouteKey, EventProposalSubmitDto body, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var result = await _proposalContract
            .SubmitClubAdminEventProposalAsync(clubRouteKey, body, actorUserId, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Success)
            throw new ArgumentException(result.ErrorMessage ?? "Invalid event proposal.");

        if (result.Payload is not Dictionary<string, object?> d)
            return new EventProposalResultDto { ProposalId = string.Empty, Id = string.Empty, Status = "pending_review", ClubId = ctx.Key };

        d.TryGetValue("proposalId", out var pid);
        d.TryGetValue("id", out var id);
        d.TryGetValue("status", out var st);
        d.TryGetValue("clubId", out var cid);
        return new EventProposalResultDto
        {
            ProposalId = pid?.ToString() ?? string.Empty,
            Id = id?.ToString() ?? pid?.ToString() ?? string.Empty,
            Status = st?.ToString() ?? "pending_review",
            ClubId = cid?.ToString() ?? ctx.Key
        };
    }

    public async Task<ClubAdminPagedResult<ClubAdminAttendeeDto>> GetEventAttendeesAsync(string clubRouteKey, string eventId, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminAttendeeDto>(page, limit);

        var clubId = ctx.ClubId;
        if (!int.TryParse(eventId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eid))
            return EmptyPage<ClubAdminAttendeeDto>(page, limit);

        var existing = await _eventService.GetByIdAsync(eid).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return EmptyPage<ClubAdminAttendeeDto>(page, limit);

        var p = Math.Max(page, 1);
        var l = Math.Max(limit, 1);
        var query = _db.EventRegistrations
            .AsNoTracking()
            .Where(r => r.EventId == eid)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var regs = await query
            .Skip((p - 1) * l)
            .Take(l)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var eventIdStr = eid.ToString(CultureInfo.InvariantCulture);
        var items = new List<ClubAdminAttendeeDto>(regs.Count);
        foreach (var r in regs)
        {
            items.Add(new ClubAdminAttendeeDto
            {
                Id = r.UserId.ToString("D"),
                EventId = eventIdStr,
                UserId = r.UserId.ToString("D"),
                Entered = r.IsCheckedIn,
                TicketId = r.TicketId ?? string.Empty,
                RegisteredAt = r.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            });
        }

        return new ClubAdminPagedResult<ClubAdminAttendeeDto> { Items = items, Page = p, Limit = l, Total = total };
    }

    public async Task<ClubAdminAttendeeDto?> PatchEventAttendeeAsync(string clubRouteKey, string eventId, string attendeeId, bool entered, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var clubId = ctx.ClubId;
        if (!int.TryParse(eventId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eid))
            return null;

        var existing = await _eventService.GetByIdAsync(eid).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return null;

        if (!Guid.TryParse(attendeeId, out var attendeeUserId))
            return null;

        var registration = await _db.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eid && r.UserId == attendeeUserId, cancellationToken)
            .ConfigureAwait(false);
        if (registration == null)
            return null;

        var now = DateTime.UtcNow;
        registration.IsCheckedIn = entered;
        registration.ModifiedAt = now;
        registration.LastScannedAt = entered ? now : null;
        registration.CheckedInAt = entered ? registration.CheckedInAt ?? now : null;
        if (!entered)
            registration.CheckedInBy = null;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ClubAdminAttendeeDto
        {
            Id = registration.UserId.ToString("D"),
            EventId = eid.ToString(CultureInfo.InvariantCulture),
            UserId = registration.UserId.ToString("D"),
            Entered = registration.IsCheckedIn,
            TicketId = registration.TicketId ?? string.Empty,
            RegisteredAt = registration.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        };
    }

    public async Task<bool> DeleteEventAttendeeAsync(string clubRouteKey, string eventId, string attendeeId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        var clubId = ctx.ClubId;
        if (!int.TryParse(eventId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eid))
            return false;

        var existing = await _eventService.GetByIdAsync(eid).ConfigureAwait(false);
        if (existing == null || existing.ClubId != clubId)
            return false;

        if (!Guid.TryParse(attendeeId, out var attendeeUserId))
            return false;

        var registration = await _db.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == eid && r.UserId == attendeeUserId, cancellationToken)
            .ConfigureAwait(false);
        if (registration == null)
            return false;

        var now = DateTime.UtcNow;
        registration.IsDeleted = true;
        registration.DeletedAt = now;
        registration.ModifiedAt = now;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<ClubResourceItemDto>> GetFilesAsync(string clubRouteKey, CancellationToken cancellationToken = default)
    {
        var entity = await TryResolveClubEntityAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return Array.Empty<ClubResourceItemDto>();

        var (resources, changed) = ClubResourcesJson.ParseForManagement(entity.ResourcesJson);
        if (changed)
            await SaveResourcesAsync(entity, resources).ConfigureAwait(false);

        return resources;
    }

    public async Task<ClubResourceItemDto?> GetFileAsync(string clubRouteKey, string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        var entity = await TryResolveClubEntityAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return null;

        var normalizedFileId = fileId.Trim();
        var (resources, changed) = ClubResourcesJson.ParseForManagement(entity.ResourcesJson);
        var match = resources.FirstOrDefault(x => string.Equals(x.Id, normalizedFileId, StringComparison.Ordinal));

        if (changed)
            await SaveResourcesAsync(entity, resources).ConfigureAwait(false);

        return match;
    }

    public async Task<ClubResourceItemDto?> CreateFileAsync(string clubRouteKey, ClubAdminCreateFileInput input, CancellationToken cancellationToken = default)
    {
        var entity = await TryResolveClubEntityAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return null;

        var (resources, _) = ClubResourcesJson.ParseForManagement(entity.ResourcesJson);
        if (resources.Count >= ClubResourcesJson.MaxItems)
            throw new InvalidOperationException($"At most {ClubResourcesJson.MaxItems} files are allowed.");

        var created = new ClubResourceItemDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Url = NormalizeResourceUrl(input.Url),
            Title = NormalizeResourceTitle(input.Title)
        };

        resources.Add(created);
        await SaveResourcesAsync(entity, resources, true).ConfigureAwait(false);
        return created;
    }

    public async Task<ClubResourceItemDto?> UpdateFileAsync(string clubRouteKey, string fileId, ClubAdminUpdateFileInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        var entity = await TryResolveClubEntityAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return null;

        var normalizedFileId = fileId.Trim();
        var (resources, changed) = ClubResourcesJson.ParseForManagement(entity.ResourcesJson);
        var existing = resources.FirstOrDefault(x => string.Equals(x.Id, normalizedFileId, StringComparison.Ordinal));
        if (existing == null)
            return null;

        var modified = changed;
        if (input.Url is not null)
        {
            var normalizedUrl = NormalizeResourceUrl(input.Url);
            if (!string.Equals(existing.Url, normalizedUrl, StringComparison.Ordinal))
            {
                existing.Url = normalizedUrl;
                modified = true;
            }
        }

        if (input.Title is not null)
        {
            var normalizedTitle = NormalizeResourceTitle(input.Title);
            if (!string.Equals(existing.Title, normalizedTitle, StringComparison.Ordinal))
            {
                existing.Title = normalizedTitle;
                modified = true;
            }
        }

        await SaveResourcesAsync(entity, resources, modified).ConfigureAwait(false);
        return existing;
    }

    public async Task<ClubResourceItemDto?> DeleteFileAsync(string clubRouteKey, string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        var entity = await TryResolveClubEntityAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (entity == null)
            return null;

        var normalizedFileId = fileId.Trim();
        var (resources, _) = ClubResourcesJson.ParseForManagement(entity.ResourcesJson);
        var removed = resources.FirstOrDefault(x => string.Equals(x.Id, normalizedFileId, StringComparison.Ordinal));
        if (removed == null)
            return null;

        resources.Remove(removed);
        await SaveResourcesAsync(entity, resources, true).ConfigureAwait(false);
        return removed;
    }

    public async Task<ProfilePatchResultDto> PatchProfileAsync(
        string clubRouteKey,
        string? socialLinks,
        string? focusAreasJson,
        string? email,
        string? resourcesJson,
        ClubProfileUploadHintsDto? uploadHints,
        CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
        {
            return new ProfilePatchResultDto
            {
                ClubId = clubRouteKey,
                Updated = false,
                PendingReview = false,
                FocusAreas = new List<ClubFocusAreaItemDto>(),
                ProfileImageUrl = null,
                BackgroundImageUrl = null,
                Email = null,
                SocialLinks = null,
                Resources = new List<ClubResourceItemDto>()
            };
        }

        var entity = await _clubRepository.GetByIdWithCategoryAsync(ctx.ClubId).ConfigureAwait(false);
        if (entity == null)
        {
            return new ProfilePatchResultDto
            {
                ClubId = clubRouteKey,
                Updated = false,
                PendingReview = false,
                FocusAreas = new List<ClubFocusAreaItemDto>(),
                ProfileImageUrl = null,
                BackgroundImageUrl = null,
                Email = null,
                SocialLinks = null,
                Resources = new List<ClubResourceItemDto>()
            };
        }

        var modified = false;

        if (focusAreasJson is not null)
        {
            if (string.IsNullOrWhiteSpace(focusAreasJson))
                throw new ArgumentException("focusAreasJson must be a valid JSON array");

            var (ok, err, serialized, _) = ClubFocusAreasJson.ValidateAndNormalizeForWrite(focusAreasJson);
            if (!ok || serialized == null)
                throw new ArgumentException(err ?? "Invalid focusAreasJson");

            entity.FocusAreasJson = serialized;
            modified = true;
        }

        if (socialLinks is not null)
        {
            if (string.IsNullOrWhiteSpace(socialLinks))
                throw new ArgumentException("socialLinks must be a valid JSON object when provided");

            var (ok, err, canonical) = ClubSocialLinksJson.MergePatch(entity.SocialLinksJson, socialLinks);
            if (!ok || canonical == null)
                throw new ArgumentException(err ?? "Invalid socialLinks");

            entity.SocialLinksJson = canonical;
            modified = true;
        }

        if (email is not null)
        {
            if (!ClubContactEmail.TryNormalize(email, out var em, out var mailErr))
                throw new ArgumentException(mailErr ?? "Invalid email.");

            entity.Email = em;
            modified = true;
        }

        if (resourcesJson is not null)
        {
            if (string.IsNullOrWhiteSpace(resourcesJson))
                throw new ArgumentException("resourcesJson must be a valid JSON array when provided");

            var (ok, err, canonical) = ClubResourcesJson.ValidateAndReplace(resourcesJson);
            if (!ok || canonical == null)
                throw new ArgumentException(err ?? "Invalid resourcesJson");

            entity.ResourcesJson = canonical;
            modified = true;
        }

        var logoUrl = uploadHints?.LogoUrl;
        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            var normalizedLogoUrl = logoUrl.Trim();
            if (!string.Equals(entity.ProfileImageUrl, normalizedLogoUrl, StringComparison.Ordinal))
            {
                entity.ProfileImageUrl = normalizedLogoUrl;
                modified = true;
            }
        }

        var backgroundUrl = uploadHints?.BackgroundUrl;
        if (!string.IsNullOrWhiteSpace(backgroundUrl))
        {
            var normalizedBackgroundUrl = backgroundUrl.Trim();
            if (!string.Equals(entity.BackgroundImageUrl, normalizedBackgroundUrl, StringComparison.Ordinal))
            {
                entity.BackgroundImageUrl = normalizedBackgroundUrl;
                modified = true;
            }
        }

        if (modified)
        {
            entity.ModifiedAt = DateTime.UtcNow;
            await _clubRepository.UpdateAsync(entity).ConfigureAwait(false);
            await _clubRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        var publishedFocus = ClubFocusAreasJson.ParseForRead(entity.FocusAreasJson);
        var publishedSocial = ClubSocialLinksJson.ParseForRead(entity.SocialLinksJson);
        if (publishedSocial is { Website: null, Instagram: null, X: null, Tiktok: null })
            publishedSocial = null;

        var publishedResources = ClubResourcesJson.ParseForRead(entity.ResourcesJson);
        return new ProfilePatchResultDto
        {
            ClubId = ctx.Key,
            Updated = modified,
            PendingReview = false,
            FocusAreas = publishedFocus,
            ProfileImageUrl = entity.ProfileImageUrl,
            BackgroundImageUrl = entity.BackgroundImageUrl,
            Email = entity.Email,
            SocialLinks = publishedSocial,
            Resources = publishedResources
        };
    }

    public async Task<ClubAdminAnnouncementDto> CreateAnnouncementAsync(string clubRouteKey, string title, string message, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false)
                  ?? throw new InvalidOperationException("Club not found.");

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var id = Guid.NewGuid().ToString("D");
        var row = new AnnouncementRow { Id = id, Title = title, Message = message, CreatedAt = DateTimeOffset.UtcNow };
        ws.Announcements[id] = row;
        return new ClubAdminAnnouncementDto
        {
            Id = row.Id,
            Title = row.Title,
            Message = row.Message,
            CreatedAt = row.CreatedAt.ToString("O")
        };
    }

    private async Task<bool> JobApplicationBelongsToClubAsync(int jobApplicationId, int clubId)
    {
        var dto = await _jobApplicationService.GetByIdAsync(jobApplicationId).ConfigureAwait(false);
        if (dto == null)
            return false;
        var vacancy = await _jobVacancyService.GetByIdAsync(dto.JobVacancyId).ConfigureAwait(false);
        return vacancy != null && vacancy.ClubId == clubId;
    }

    private static ClubAdminVacancyAdminDto MapVacancyAdminDto(JobVacancyDto v, string clubRouteKey) =>
        new()
        {
            Id = v.Id.ToString(CultureInfo.InvariantCulture),
            ClubId = clubRouteKey,
            Title = v.Title,
            Description = v.Description,
            PostedBy = v.PostedBy.ToString("D"),
            Status = v.IsDraft ? "draft" : v.IsActive ? "active" : "inactive",
            PositionId = v.ClubPositionId.HasValue
                ? v.ClubPositionId.Value.ToString(CultureInfo.InvariantCulture)
                : null,
            ApplicationDeadline = v.ApplicationDeadline,
            IsDraft = v.IsDraft
        };

    private static ClubAdminPositionDto MapPositionDto(ClubPosition p, string clubRouteKey)
    {
        var reqs = p.Requirements.OrderBy(r => r.SortOrder).Select(r => r.Text).ToList();
        return new ClubAdminPositionDto
        {
            Id = p.Id.ToString(CultureInfo.InvariantCulture),
            ClubId = clubRouteKey,
            Name = p.Title,
            Title = p.Title,
            Category = p.Category,
            CategoryName = GetPositionCategoryName(p.Category),
            Requirements = reqs
        };
    }

    private static ClubPositionCategory EnsureValidPositionCategory(ClubPositionCategory category)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentException("Invalid position category.");

        return category;
    }

    private static string GetPositionCategoryName(ClubPositionCategory category) =>
        Enum.IsDefined(category) ? category.ToString() : ClubPositionCategory.Other.ToString();

    private async Task<ClubRouteContext?> TryResolveClubAsync(string clubRouteKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ClubRouteId.TryParseClubKey(clubRouteKey, out var id))
            return null;
        var club = await _clubService.GetByIdAsync(id).ConfigureAwait(false);
        return club == null ? null : new ClubRouteContext(id, clubRouteKey.Trim());
    }

    private async Task<Club?> TryResolveClubEntityAsync(string clubRouteKey, CancellationToken cancellationToken)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        return await _clubRepository.GetByIdWithCategoryAsync(ctx.ClubId).ConfigureAwait(false);
    }

    private async Task SaveResourcesAsync(Club entity, IReadOnlyCollection<ClubResourceItemDto> resources, bool modified = true)
    {
        if (!modified)
            return;

        entity.ResourcesJson = ClubResourcesJson.SerializeCanonical(resources);
        entity.ModifiedAt = DateTime.UtcNow;
        await _clubRepository.UpdateAsync(entity).ConfigureAwait(false);
        await _clubRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> TryGetUserNamesAsync(
        IEnumerable<Guid> userIds,
        string clubRouteKey,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _authUserLookup.GetUserNamesByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        }
        catch (NpgsqlException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Unable to resolve auth user names for club {ClubRouteKey}. Returning employee records without names.",
                clubRouteKey);
            return new Dictionary<Guid, string>();
        }
    }

    private static string NormalizeResourceUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("File URL is required.");

        if (!ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(url, out var normalized, out var error))
            throw new ArgumentException(error ?? "Invalid file URL.");

        return normalized;
    }

    private static string? NormalizeResourceTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? null : title.Trim();

    private static string AttendeeBucketKey(int clubId, int eventId) => $"{clubId.ToString(CultureInfo.InvariantCulture)}:{eventId.ToString(CultureInfo.InvariantCulture)}";

    private IReadOnlyList<ClubAdminApplicationNoteDto> SnapshotNotesForClub(int clubId, string applicationId)
    {
        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.ApplicationNotes.TryGetValue(applicationId, out var q))
            return Array.Empty<ClubAdminApplicationNoteDto>();
        return q.ToArray().Select(n => new ClubAdminApplicationNoteDto { Text = n.Text, CreatedAt = n.CreatedAt.ToString("O") }).ToList();
    }

    private static ClubAdminPagedResult<T> Page<T>(IEnumerable<T> items, int page, int limit)
    {
        var list = items.ToList();
        return Page(list, page, limit);
    }

    private static ClubAdminPagedResult<T> Page<T>(IReadOnlyList<T> list, int page, int limit)
    {
        var p = Math.Max(page, 1);
        var l = Math.Max(limit, 1);
        var total = list.Count;
        var slice = list.Skip((p - 1) * l).Take(l).ToList();
        return new ClubAdminPagedResult<T> { Items = slice, Page = p, Limit = l, Total = total };
    }

    private static ClubAdminPagedResult<T> EmptyPage<T>(int page, int limit) =>
        new() { Items = Array.Empty<T>(), Page = Math.Max(page, 1), Limit = Math.Max(limit, 1), Total = 0 };

    private static bool TryBuildSlotSeries(GenerateInterviewSlotsInput input, out List<(DateTimeOffset Start, DateTimeOffset End)> slots)
    {
        slots = new List<(DateTimeOffset, DateTimeOffset)>();
        if (!DateOnly.TryParse(input.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDay))
            return false;
        if (!TimeOnly.TryParse(input.StartTime, CultureInfo.InvariantCulture, out var startT))
            return false;
        if (!TimeOnly.TryParse(input.EndTime, CultureInfo.InvariantCulture, out var endT))
            return false;

        var lastDay = firstDay;
        if (!string.IsNullOrWhiteSpace(input.EndDate)
            && DateOnly.TryParse(input.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
        {
            lastDay = parsedEnd < firstDay ? firstDay : parsedEnd;
        }

        var len = Math.Max(input.InterviewLengthMinutes, 5);
        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            var cursor = new DateTimeOffset(day.ToDateTime(startT), TimeSpan.Zero);
            var dayEndBoundary = new DateTimeOffset(day.ToDateTime(endT), TimeSpan.Zero);
            while (cursor.AddMinutes(len) <= dayEndBoundary)
            {
                var next = cursor.AddMinutes(len);
                slots.Add((cursor, next));
                cursor = next;
            }
        }

        return slots.Count > 0;
    }
}
