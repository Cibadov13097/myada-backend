using System.Globalization;
using ClubManagementService.Application.ClubAdmin;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Infrastructure.Services;

public sealed class ClubAdminService : IClubAdminService
{
    private sealed record ClubRouteContext(int ClubId, string Key);
    private readonly IClubService _clubService;
    private readonly IJobVacancyService _jobVacancyService;
    private readonly IJobApplicationService _jobApplicationService;
    private readonly IEventService _eventService;
    private readonly IClubPositionRepository _positionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ClubAdminWorkspaceStore _workspace;

    public ClubAdminService(
        IClubService clubService,
        IJobVacancyService jobVacancyService,
        IJobApplicationService jobApplicationService,
        IEventService eventService,
        IClubPositionRepository positionRepository,
        ICategoryRepository categoryRepository,
        ClubAdminWorkspaceStore workspace)
    {
        _clubService = clubService;
        _jobVacancyService = jobVacancyService;
        _jobApplicationService = jobApplicationService;
        _eventService = eventService;
        _positionRepository = positionRepository;
        _categoryRepository = categoryRepository;
        _workspace = workspace;
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
        var newApps = jobApps.Count(a => a.Status is JobApplicationStatus.Pending or JobApplicationStatus.Reviewing);
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
                         .Where(m => ClubAdminStatusMaps.MembershipMatchesApiFilter(status, m.Status)))
            {
                list.Add(new ClubAdminApplicationListItemDto
                {
                    Id = ClubAdminApplicationId.Membership(m.Id),
                    ApplicationType = "membership",
                    Status = ClubAdminStatusMaps.NormalizeMembershipStatus(m.Status),
                    Membership = new ClubAdminMembershipApplicationBriefDto
                    {
                        ApplicantUserId = m.ApplicantUserId,
                        Message = m.Message
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
                    CreatedAt = dto.CreatedAt.ToUniversalTime().ToString("O")
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
                Message = row.Message,
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

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var items = ws.InterviewSlots.Values
            .OrderBy(s => s.StartsAt)
            .Select(s => new ClubAdminInterviewSlotDto
            {
                Id = s.Id,
                ClubId = ctx.Key,
                StartsAt = s.StartsAt,
                EndsAt = s.EndsAt,
                VacancyId = s.VacancyId,
                Capacity = s.Capacity
            })
            .ToList();

        return Page(items, page, limit);
    }

    public async Task<ClubAdminPagedResult<ClubAdminInterviewSlotDto>> GenerateInterviewSlotsAsync(string clubRouteKey, GenerateInterviewSlotsInput input, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminInterviewSlotDto>(1, 20);

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var created = new List<ClubAdminInterviewSlotDto>();
        if (TryBuildSlotSeries(input, out var slots))
        {
            foreach (var slot in slots)
            {
                var id = Guid.NewGuid().ToString("D");
                ws.InterviewSlots[id] = new InterviewSlotRow
                {
                    Id = id,
                    StartsAt = slot.Start,
                    EndsAt = slot.End,
                    VacancyId = input.VacancyId,
                    Capacity = 1
                };
                created.Add(new ClubAdminInterviewSlotDto
                {
                    Id = id,
                    ClubId = ctx.Key,
                    StartsAt = slot.Start,
                    EndsAt = slot.End,
                    VacancyId = input.VacancyId,
                    Capacity = 1
                });
            }
        }

        return Page(created, 1, Math.Max(created.Count, 1));
    }

    public async Task<ClubAdminInterviewSlotDto?> PatchInterviewSlotAsync(string clubRouteKey, string slotId, PatchInterviewSlotInput input, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        if (!ws.InterviewSlots.TryGetValue(slotId, out var row))
            return null;

        if (input.StartsAt.HasValue)
            row.StartsAt = input.StartsAt.Value;
        if (input.EndsAt.HasValue)
            row.EndsAt = input.EndsAt.Value;
        if (input.VacancyId != null)
            row.VacancyId = input.VacancyId;
        if (input.Capacity.HasValue)
            row.Capacity = input.Capacity.Value;

        return new ClubAdminInterviewSlotDto
        {
            Id = row.Id,
            ClubId = ctx.Key,
            StartsAt = row.StartsAt,
            EndsAt = row.EndsAt,
            VacancyId = row.VacancyId,
            Capacity = row.Capacity
        };
    }

    public async Task<bool> DeleteInterviewSlotAsync(string clubRouteKey, string slotId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        return ws.InterviewSlots.TryRemove(slotId, out _);
    }

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

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId).ConfigureAwait(false);
        if (category == null)
            throw new ArgumentException("Invalid category.");

        var clubId = ctx.ClubId;
        var key = ctx.Key;
        var position = new ClubPosition
        {
            ClubId = clubId,
            Title = dto.Title.Trim(),
            CategoryId = dto.CategoryId
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

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId).ConfigureAwait(false);
        if (category == null)
            throw new ArgumentException("Invalid category.");

        if (!string.IsNullOrWhiteSpace(dto.Title))
            entity.Title = dto.Title.Trim();
        entity.CategoryId = dto.CategoryId;
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

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var items = ws.Members.Values
            .Select(m => new ClubAdminMemberDto { Id = m.Id, ClubId = ctx.Key, UserId = m.UserId })
            .ToList();
        return Page(items, page, limit);
    }

    public async Task<bool> DeleteMemberAsync(string clubRouteKey, string memberId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        return ws.Members.TryRemove(memberId, out _);
    }

    public async Task<ClubAdminPagedResult<ClubAdminEmployeeDto>> GetEmployeesAsync(string clubRouteKey, int page, int limit, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return EmptyPage<ClubAdminEmployeeDto>(page, limit);

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var items = ws.Employees.Values
            .Select(e => new ClubAdminEmployeeDto
            {
                Id = e.Id,
                ClubId = ctx.Key,
                StudentId = e.StudentId,
                PositionId = e.PositionId
            })
            .ToList();
        return Page(items, page, limit);
    }

    public async Task<ClubAdminEmployeeDto> CreateEmployeeAsync(string clubRouteKey, string studentId, string positionId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false)
                  ?? throw new InvalidOperationException("Club not found.");

        if (int.TryParse(positionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
        {
            var pos = await _positionRepository
                .GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken)
                .ConfigureAwait(false);
            if (pos == null)
                throw new ArgumentException("Position not found for this club.");
        }

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        var id = Guid.NewGuid().ToString("D");
        ws.Employees[id] = new EmployeeRow { Id = id, StudentId = studentId, PositionId = positionId };
        return new ClubAdminEmployeeDto
        {
            Id = id,
            ClubId = ctx.Key,
            StudentId = studentId,
            PositionId = positionId
        };
    }

    public async Task PatchEmployeePositionsAsync(string clubRouteKey, IReadOnlyList<EmployeePositionChange> changes, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        foreach (var c in changes)
        {
            if (int.TryParse(c.PositionId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pid))
            {
                var pos = await _positionRepository
                    .GetByIdWithDetailsAsync(pid, ctx.ClubId, cancellationToken)
                    .ConfigureAwait(false);
                if (pos == null)
                    throw new ArgumentException("Position not found for this club.");
            }

            if (ws.Employees.TryGetValue(c.EmployeeId, out var row))
                row.PositionId = c.PositionId;
        }
    }

    public async Task<bool> DeleteEmployeeAsync(string clubRouteKey, string employeeId, CancellationToken cancellationToken = default)
    {
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return false;

        var ws = _workspace.GetOrCreate(ctx.ClubId);
        return ws.Employees.TryRemove(employeeId, out _);
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

    public async Task<EventProposalResultDto?> CreateEventProposalAsync(string clubRouteKey, object body, CancellationToken cancellationToken = default)
    {
        _ = body;
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return null;

        return new EventProposalResultDto { ProposalId = Guid.NewGuid().ToString("D") };
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

        var ws = _workspace.GetOrCreate(clubId);
        var bucket = ws.EventAttendees.GetOrAdd(AttendeeBucketKey(clubId, eid), _ => new AttendeeBucket());
        var items = bucket.Items.Values
            .Select(a => new ClubAdminAttendeeDto
            {
                Id = a.Id,
                EventId = eid.ToString(CultureInfo.InvariantCulture),
                UserId = a.UserId,
                Entered = a.Entered
            })
            .ToList();
        return Page(items, page, limit);
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

        var ws = _workspace.GetOrCreate(clubId);
        var bucket = ws.EventAttendees.GetOrAdd(AttendeeBucketKey(clubId, eid), _ => new AttendeeBucket());
        var row = bucket.Items.GetOrAdd(attendeeId, _ => new AttendeeRow { Id = attendeeId, UserId = attendeeId });
        row.Entered = entered;
        return new ClubAdminAttendeeDto
        {
            Id = row.Id,
            EventId = eid.ToString(CultureInfo.InvariantCulture),
            UserId = row.UserId,
            Entered = row.Entered
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

        var ws = _workspace.GetOrCreate(clubId);
        if (!ws.EventAttendees.TryGetValue(AttendeeBucketKey(clubId, eid), out var bucket))
            return false;
        return bucket.Items.TryRemove(attendeeId, out _);
    }

    public async Task<ProfilePatchResultDto> PatchProfileAsync(string clubRouteKey, string? socialLinks, string? focusAreasJson, CancellationToken cancellationToken = default)
    {
        _ = socialLinks;
        _ = focusAreasJson;
        var ctx = await TryResolveClubAsync(clubRouteKey, cancellationToken).ConfigureAwait(false);
        if (ctx == null)
            return new ProfilePatchResultDto { ClubId = clubRouteKey, Updated = false };

        return new ProfilePatchResultDto { ClubId = ctx.Key, Updated = true };
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
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Requirements = reqs
        };
    }

    private async Task<ClubRouteContext?> TryResolveClubAsync(string clubRouteKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ClubRouteId.TryParseClubKey(clubRouteKey, out var id))
            return null;
        var club = await _clubService.GetByIdAsync(id).ConfigureAwait(false);
        return club == null ? null : new ClubRouteContext(id, clubRouteKey.Trim());
    }

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
        if (!DateOnly.TryParse(input.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return false;
        if (!TimeOnly.TryParse(input.StartTime, CultureInfo.InvariantCulture, out var startT))
            return false;
        if (!TimeOnly.TryParse(input.EndTime, CultureInfo.InvariantCulture, out var endT))
            return false;

        var len = Math.Max(input.InterviewLengthMinutes, 5);
        var cursor = new DateTimeOffset(day.ToDateTime(startT), TimeSpan.Zero);
        var end = new DateTimeOffset(day.ToDateTime(endT), TimeSpan.Zero);
        while (cursor.AddMinutes(len) <= end)
        {
            var next = cursor.AddMinutes(len);
            slots.Add((cursor, next));
            cursor = next;
        }

        return slots.Count > 0;
    }
}
