using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClubManagementService.Application.ClubAdmin;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Services;

public sealed class ProposalContractService : IProposalContractService
{
    private static readonly Regex EightDigitStudent = new(@"^\d{8}$", RegexOptions.Compiled);

    private readonly ProposalStateStore _store;
    private readonly IClubService _clubService;
    private readonly IEventService _eventService;
    private readonly IClubMembershipRepository _clubMembershipRepository;
    private readonly ClubManagementDbContext _db;

    public ProposalContractService(
        ProposalStateStore store,
        IClubService clubService,
        IEventService eventService,
        IClubMembershipRepository clubMembershipRepository,
        ClubManagementDbContext db)
    {
        _store = store;
        _clubService = clubService;
        _eventService = eventService;
        _clubMembershipRepository = clubMembershipRepository;
        _db = db;
    }

    public async Task<ClubProposalSubmitResult> SubmitClubProposalAsync(
        string? userId,
        ClubProposalSubmissionDto submission,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var name = submission.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            AddErr(errors, nameof(submission.Name), "Club name is required.");

        var description = submission.Description?.Trim() ?? string.Empty;
        EnrichFromDescription(submission.Description, submission);

        var hasStructured =
            !string.IsNullOrWhiteSpace(submission.PresidentStudentId)
            || !string.IsNullOrWhiteSpace(submission.VicePresidentStudentId)
            || HasOtherMembers(submission.OtherMembersJson)
            || !string.IsNullOrWhiteSpace(submission.Commitment)
            || !string.IsNullOrWhiteSpace(submission.ShortDesc)
            || !string.IsNullOrWhiteSpace(submission.UniqueDesc)
            || !string.IsNullOrWhiteSpace(submission.Goals)
            || !string.IsNullOrWhiteSpace(submission.Activities)
            || !string.IsNullOrWhiteSpace(submission.Alignment)
            || !string.IsNullOrWhiteSpace(submission.Vision);

        if (hasStructured)
            ValidateStructured(errors, submission);

        if (string.IsNullOrEmpty(description))
        {
            description = BuildDescriptionFromParts(
                submission.ShortDesc,
                submission.UniqueDesc,
                submission.Goals,
                submission.Activities,
                submission.Alignment,
                submission.Vision);
        }

        if (!hasStructured && string.IsNullOrEmpty(description))
            AddErr(errors, nameof(submission.Description), "Description is required.");

        if (description.Length > 12000)
            AddErr(errors, nameof(submission.Description), "Description must be at most 12000 characters.");

        ValidateLogo(errors, submission.LogoFile);
        ValidateConstitution(errors, submission.ConstitutionFile);

        if (errors.Count > 0)
        {
            return new ClubProposalSubmitResult
            {
                Success = false,
                ErrorBody = new { message = "Validation failed.", errors },
                ErrorMessage = "Validation failed."
            };
        }

        var proposalId = _store.NextClubProposalId();
        var id = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;

        var shortDesc = submission.ShortDesc?.Trim();
        var uniqueDesc = submission.UniqueDesc?.Trim();
        var goals = submission.Goals?.Trim();
        var activities = submission.Activities?.Trim();
        var alignment = submission.Alignment?.Trim();
        var vision = submission.Vision?.Trim();

        var primary = new List<PrimaryOfficer>();
        if (!string.IsNullOrWhiteSpace(submission.PresidentStudentId))
            primary.Add(new PrimaryOfficer { Role = "President", StudentId = submission.PresidentStudentId!.Trim() });
        if (!string.IsNullOrWhiteSpace(submission.VicePresidentStudentId))
            primary.Add(new PrimaryOfficer { Role = "Vice President", StudentId = submission.VicePresidentStudentId!.Trim() });

        var others = ParseOtherMembers(submission.OtherMembersJson);

        var record = new ClubProposalRecord
        {
            Id = id,
            ProposalId = proposalId,
            SubmittedByUserId = userId?.Trim() ?? string.Empty,
            Name = name,
            Description = description,
            ShortDesc = shortDesc,
            UniqueDesc = uniqueDesc,
            Goals = goals,
            Activities = activities,
            Alignment = alignment,
            Vision = vision,
            Commitment = submission.Commitment?.Trim().ToLowerInvariant(),
            PrimaryOfficers = primary,
            OtherMembers = others,
            SubmittedAt = now,
            LogoUrl = submission.LogoFile!.Trim(),
            ConstitutionDoc = new ConstitutionDoc
            {
                Name = ResolveAttachmentName(submission.ConstitutionFilePart, submission.ConstitutionFile, "constitution.pdf"),
                SizeBytes = submission.ConstitutionFilePart?.SizeBytes ?? 0,
                UploadedAt = now,
                Url = submission.ConstitutionFile!.Trim()
            }
        };

        _store.AddClubProposal(record);

        await Task.Yield();

        return new ClubProposalSubmitResult
        {
            Success = true,
            Payload = new
            {
                proposalId,
                id = proposalId,
                status = "pending_review",
                submittedAt = now.ToUniversalTime().ToString("O")
            }
        };
    }

    public Task<object> ListClubProposalsAsync(string? status, string? search, int page, int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = _store.AllClubProposals().AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            list = list.Where(x => string.Equals(x.Status, s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            list = list.Where(x =>
                x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.ProposalId.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = list.ToList();
        var p = Math.Max(page, 1);
        var l = Math.Max(limit, 1);
        var total = materialized.Count;
        var slice = materialized.Skip((p - 1) * l).Take(l).Select(MapClubProposal).ToList();
        return Task.FromResult<object>(new { items = slice, page = p, totalCount = total });
    }

    public Task<object?> GetClubProposalAsync(string proposalIdOrInternalId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var r = _store.FindClubProposal(proposalIdOrInternalId);
        return Task.FromResult<object?>(r == null ? null : MapClubProposal(r));
    }

    public async Task<object?> ApproveClubProposalAsync(string proposalIdOrInternalId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var r = _store.FindClubProposal(proposalIdOrInternalId);
        if (r == null)
            return null;

        // Idempotency: if already approved, don't create another club.
        if (string.Equals(r.Status, "approved", StringComparison.OrdinalIgnoreCase) && r.ApprovedClubId.HasValue)
        {
            return new
            {
                clubId = r.ApprovedClubId.Value.ToString(CultureInfo.InvariantCulture),
                proposalId = r.ProposalId,
                status = "approved"
            };
        }

        var clubs = await _clubService.GetAllAsync().ConfigureAwait(false);
        var categoryId = clubs.FirstOrDefault()?.CategoryId ?? 1;

        var created = await _clubService.CreateAsync(new CreateClubDto
        {
            Name = r.Name,
            Description = r.Description,
            ApprovedBy = actorUserId,
            CategoryId = categoryId
        }).ConfigureAwait(false);

        await AddOtherMembersToClubAsync(created.Id, r.OtherMembers, actorUserId, cancellationToken).ConfigureAwait(false);

        r.Status = "approved";
        r.ApprovedClubId = created.Id;

        return new
        {
            clubId = created.Id.ToString(CultureInfo.InvariantCulture),
            proposalId = r.ProposalId,
            status = "approved"
        };
    }

    public Task<object?> RejectClubProposalAsync(string proposalIdOrInternalId, string reason, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        _ = actorUserId;
        cancellationToken.ThrowIfCancellationRequested();
        var r = _store.FindClubProposal(proposalIdOrInternalId);
        if (r == null)
            return Task.FromResult<object?>(null);
        r.Status = "rejected";
        _ = reason;
        return Task.FromResult<object?>(new { status = "rejected" });
    }

    public Task<object?> RequestClubProposalRevisionAsync(string proposalIdOrInternalId, string changes, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        _ = changes;
        _ = actorUserId;
        cancellationToken.ThrowIfCancellationRequested();
        var r = _store.FindClubProposal(proposalIdOrInternalId);
        if (r == null)
            return Task.FromResult<object?>(null);
        r.Status = "under_revision";
        return Task.FromResult<object?>(new { status = "under_revision" });
    }

    public ClubProposalRequirementsDto GetClubProposalRequirements() => _store.GetRequirements();

    public ClubProposalRequirementsDto SetClubProposalRequirements(ClubProposalRequirementsDto dto) => _store.SetRequirements(dto);

    public Task<EventProposalSubmitResult> SubmitStaffEventProposalAsync(
        EventProposalSubmitDto body,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var err = ValidateEventBody(body);
        if (err != null)
        {
            return Task.FromResult(new EventProposalSubmitResult
            {
                Success = false,
                ErrorBody = new { message = err },
                ErrorMessage = err
            });
        }

        var now = DateTimeOffset.UtcNow;

        var record = new EventProposal
        {
            ClubNumericId = null,
            ClubRouteKey = null,
            ClubName = body.SubmittedByOrganization ?? "Student Services",
            BodyJson = JsonSerializer.Serialize(body, ProposalJson.Options),
            Status = "pending_review",
            SubmittedAt = now,
            SubmittedByUserId = actorUserId
        };

        _db.EventProposals.Add(record);
        return SaveAndReturnSubmitResultAsync(record, cancellationToken);
    }

    private async Task<EventProposalSubmitResult> SaveAndReturnSubmitResultAsync(EventProposal record, CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var proposalId = record.ProposalId;
        return new EventProposalSubmitResult
        {
            Success = true,
            Payload = new Dictionary<string, object?>
            {
                ["proposalId"] = proposalId,
                ["id"] = proposalId,
                ["status"] = "pending_review",
                ["clubId"] = record.ClubRouteKey
            }
        };
    }

    public async Task<EventProposalSubmitResult> SubmitClubAdminEventProposalAsync(
        string clubRouteKey,
        EventProposalSubmitDto body,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ClubRouteId.TryParseClubKey(clubRouteKey, out var clubNum))
        {
            return new EventProposalSubmitResult
            {
                Success = false,
                ErrorBody = new { message = "Invalid club id." },
                ErrorMessage = "Invalid club id."
            };
        }

        var err = ValidateEventBody(body);
        if (err != null)
        {
            return new EventProposalSubmitResult
            {
                Success = false,
                ErrorBody = new { message = err },
                ErrorMessage = err
            };
        }

        var club = await _clubService.GetByIdAsync(clubNum).ConfigureAwait(false);
        if (club == null)
        {
            return new EventProposalSubmitResult
            {
                Success = false,
                ErrorBody = new { message = "Club not found." },
                ErrorMessage = "Club not found."
            };
        }

        var now = DateTimeOffset.UtcNow;

        var record = new EventProposal
        {
            ClubNumericId = clubNum,
            ClubRouteKey = clubRouteKey.Trim(),
            ClubName = club.Name,
            BodyJson = JsonSerializer.Serialize(body, ProposalJson.Options),
            Status = "pending_review",
            SubmittedAt = now,
            SubmittedByUserId = actorUserId
        };

        _db.EventProposals.Add(record);
        return await SaveAndReturnSubmitResultAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public Task<object> ListEventProposalsAsync(string? status, string? search, int page, int limit, CancellationToken cancellationToken = default)
    {
        return ListEventProposalsFromDbAsync(status, search, page, limit, cancellationToken);
    }

    private async Task<object> ListEventProposalsFromDbAsync(string? status, string? search, int page, int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var q = _db.EventProposals.AsNoTracking();

        // Default queue behavior: hide archived proposals unless an explicit status filter is provided.
        if (string.IsNullOrWhiteSpace(status))
            q = q.Where(x => x.ArchivedAt == null);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            q = q.Where(x => x.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                x.ClubName.Contains(s)
                || x.Id.ToString().Contains(s)
                || x.BodyJson.Contains(s));
        }

        var p = Math.Max(page, 1);
        var l = Math.Max(limit, 1);
        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await q
            .OrderByDescending(x => x.SubmittedAt)
            .Skip((p - 1) * l)
            .Take(l)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(MapEventProposal).ToList();
        return new { items, page = p, totalCount = total };
    }

    public Task<object?> GetEventProposalAsync(string proposalIdOrInternalId, CancellationToken cancellationToken = default)
    {
        return GetEventProposalFromDbAsync(proposalIdOrInternalId, cancellationToken);
    }

    private async Task<object?> GetEventProposalFromDbAsync(string proposalIdOrInternalId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!int.TryParse(proposalIdOrInternalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;
        var r = await _db.EventProposals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        return r == null ? null : MapEventProposal(r);
    }

    public async Task<object?> ApproveEventProposalAsync(
        string proposalIdOrInternalId,
        EventProposalApproveDto body,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!int.TryParse(proposalIdOrInternalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;

        var r = await _db.EventProposals.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r == null)
            return null;

        if (string.Equals(r.Status, "approved", StringComparison.OrdinalIgnoreCase) && r.CreatedEventId.HasValue)
        {
            return new
            {
                eventId = r.CreatedEventId.Value.ToString(CultureInfo.InvariantCulture),
                proposalId = r.ProposalId,
                status = "approved"
            };
        }

        var proposalBody = JsonSerializer.Deserialize<EventProposalSubmitDto>(r.BodyJson, ProposalJson.Options) ?? new EventProposalSubmitDto();
        var subCount = proposalBody.SubEvents?.Count ?? 0;
        if (subCount > 0)
        {
            if (body.Assignments == null || body.Assignments.Count == 0)
                throw new ArgumentException("Assignments are required for proposals that include sub-events.");

            var byIndex = body.Assignments.GroupBy(a => a.SubEventIndex).ToDictionary(g => g.Key, g => g.First());
            for (var i = 0; i < subCount; i++)
            {
                if (!byIndex.TryGetValue(i, out var a)
                    || string.IsNullOrWhiteSpace(a.BuildingId)
                    || string.IsNullOrWhiteSpace(a.RoomId))
                {
                    throw new ArgumentException(
                        $"Each sub-event index must have a buildingId and roomId assignment (missing or incomplete for index {i}).");
                }
            }
        }

        if (!TryParseEventSchedule(proposalBody, out var start, out var end, out var parseErr))
            throw new ArgumentException(parseErr ?? "Invalid schedule.");

        var clubId = r.ClubNumericId ?? (await _clubService.GetAllAsync().ConfigureAwait(false)).FirstOrDefault()?.Id ?? 1;

        var seat = ParseSeat(proposalBody.Attendance);
        var requirements = SerializeLogistics(proposalBody.Logistics);
        var notes = $"Objectives: {proposalBody.Objectives}\nSub-events: {JsonSerializer.Serialize(proposalBody.SubEvents ?? new List<EventProposalSubEventDto>(), ProposalJson.Options)}";

        var created = await _eventService.CreateAsync(new CreateEventDto
        {
            ClubId = clubId,
            Name = proposalBody.Name,
            Description = proposalBody.Description,
            StartTime = start,
            EndTime = end,
            Location = proposalBody.Venue,
            SeatLimit = seat,
            Requirements = requirements,
            Prerequisites = string.Empty,
            Notes = notes,
            ApprovedBy = actorUserId
        }).ConfigureAwait(false);

        r.Status = "approved";
        r.CreatedEventId = created.Id;
        r.LastApproveAssignmentsJson = JsonSerializer.Serialize(body.Assignments ?? new List<EventProposalAssignmentDto>(), ProposalJson.Options);
        r.ArchivedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new
        {
            eventId = created.Id.ToString(CultureInfo.InvariantCulture),
            proposalId = r.ProposalId,
            status = "approved"
        };
    }

    public Task<object?> RejectEventProposalAsync(string proposalIdOrInternalId, string reason, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return RejectEventProposalDbAsync(proposalIdOrInternalId, reason, actorUserId, cancellationToken);
    }

    private async Task<object?> RejectEventProposalDbAsync(string proposalIdOrInternalId, string reason, Guid actorUserId, CancellationToken cancellationToken)
    {
        _ = reason;
        _ = actorUserId;
        cancellationToken.ThrowIfCancellationRequested();
        if (!int.TryParse(proposalIdOrInternalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;
        var r = await _db.EventProposals.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r == null)
            return null;
        r.Status = "rejected";
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new { status = "rejected" };
    }

    public Task<object?> RequestEventProposalRevisionAsync(string proposalIdOrInternalId, string changes, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        return RequestEventProposalRevisionDbAsync(proposalIdOrInternalId, changes, actorUserId, cancellationToken);
    }

    private async Task<object?> RequestEventProposalRevisionDbAsync(string proposalIdOrInternalId, string changes, Guid actorUserId, CancellationToken cancellationToken)
    {
        _ = changes;
        _ = actorUserId;
        cancellationToken.ThrowIfCancellationRequested();
        if (!int.TryParse(proposalIdOrInternalId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return null;
        var r = await _db.EventProposals.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (r == null)
            return null;
        r.Status = "under_revision";
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new { status = "under_revision" };
    }

    private static object MapClubProposal(ClubProposalRecord r)
    {
        var submittedBy = string.IsNullOrWhiteSpace(r.SubmittedByUserId) ? "User" : r.SubmittedByUserId;
        return new
        {
            id = r.Id,
            proposalId = r.ProposalId,
            name = r.Name,
            clubName = r.Name,
            shortName = (string?)null,
            shortDesc = r.ShortDesc ?? string.Empty,
            uniqueDesc = r.UniqueDesc ?? string.Empty,
            goals = r.Goals ?? string.Empty,
            activities = r.Activities ?? string.Empty,
            category = r.Category ?? "GENERAL",
            submittedBy,
            submittedAt = r.SubmittedAt.ToUniversalTime().ToString("O"),
            status = r.Status,
            primaryOfficers = r.PrimaryOfficers.Select(o => new { role = o.Role, studentId = o.StudentId }).ToList(),
            otherMembers = r.OtherMembers.Select(o => new { userId = o.UserId, status = o.Status }).ToList(),
            alignment = r.Alignment ?? string.Empty,
            vision = r.Vision ?? string.Empty,
            commitment = r.Commitment ?? string.Empty,
            constitutionDoc = new
            {
                r.ConstitutionDoc.Name,
                r.ConstitutionDoc.SizeBytes,
                uploadedAt = r.ConstitutionDoc.UploadedAt.ToUniversalTime().ToString("O"),
                url = r.ConstitutionDoc.Url
            },
            logoUrl = r.LogoUrl
        };
    }

    private async Task AddOtherMembersToClubAsync(
        int clubId,
        IReadOnlyCollection<OtherOfficer> members,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (members.Count == 0)
            return;

        var existingUserIds = await _clubMembershipRepository.GetUserIdsByClubAsync(clubId, cancellationToken).ConfigureAwait(false);
        var existing = new HashSet<Guid>(existingUserIds);
        var toInsert = members
            .Select(m => Guid.TryParse(m.UserId, out var userId) ? userId : Guid.Empty)
            .Where(userId => userId != Guid.Empty && !existing.Contains(userId))
            .Distinct()
            .ToList();

        foreach (var userId in toInsert)
        {
            await _clubMembershipRepository.AddAsync(new ClubMembership
            {
                ClubId = clubId,
                UserId = userId,
                CreatedByUserId = actorUserId
            }).ConfigureAwait(false);
        }

        if (toInsert.Count > 0)
            await _clubMembershipRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    private static object MapEventProposal(EventProposal r)
    {
        var b = JsonSerializer.Deserialize<EventProposalSubmitDto>(r.BodyJson, ProposalJson.Options) ?? new EventProposalSubmitDto();
        var (eventDateShort, timeRange, durationLabel) = FormatScheduleLabels(b);
        return new
        {
            id = r.ProposalId,
            proposalId = r.ProposalId,
            name = b.Name,
            eventTitle = b.Name,
            title = b.Name,
            clubName = r.ClubName ?? string.Empty,
            status = r.Status,
            submittedAt = r.SubmittedAt.ToUniversalTime().ToString("O"),
            submittedOn = r.SubmittedAt.ToUniversalTime().ToString("O"),
            submittedBy = r.SubmittedByUserId.ToString("D"),
            submittedAgo = string.Empty,
            dateTime = b.DateTime,
            eventDateShort,
            timeRange,
            duration = b.Duration,
            durationLabel,
            attendance = b.Attendance,
            attendanceNote = (string?)null,
            venue = b.Venue,
            venueRoom = (string?)null,
            requestedBuildingId = (string?)null,
            requestedRoomId = (string?)null,
            description = b.Description,
            objectives = b.Objectives,
            subEvents = b.SubEvents ?? new List<EventProposalSubEventDto>(),
            logistics = b.Logistics ?? new EventProposalLogisticsDto(),
            submittedByOrganization = b.SubmittedByOrganization,
            posterUrl = (string?)null,
            clubId = r.ClubRouteKey
        };
    }

    private static (string eventDateShort, string timeRange, string durationLabel) FormatScheduleLabels(EventProposalSubmitDto b)
    {
        if (!TryParseEventSchedule(b, out var start, out var end, out _))
            return (string.Empty, string.Empty, b.Duration);
        return (
            start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            $"{start:HH:mm}-{end:HH:mm}",
            string.IsNullOrWhiteSpace(b.Duration) ? string.Empty : $"{b.Duration} h");
    }

    private static EventProposalSubmitDto CloneBody(EventProposalSubmitDto b) =>
        JsonSerializer.Deserialize<EventProposalSubmitDto>(JsonSerializer.Serialize(b, ProposalJson.Options), ProposalJson.Options) ?? new EventProposalSubmitDto();

    private static string? ValidateEventBody(EventProposalSubmitDto body)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return "Event name is required.";
        if (string.IsNullOrWhiteSpace(body.DateTime))
            return "dateTime is required.";
        return null;
    }

    private static bool TryParseEventSchedule(EventProposalSubmitDto body, out DateTime start, out DateTime end, out string? error)
    {
        start = default;
        end = default;
        error = null;
        if (!DateTime.TryParse(body.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
        {
            error = "Could not parse dateTime.";
            return false;
        }

        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);

        var hours = 1;
        if (int.TryParse(body.Duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) && h > 0)
            hours = h;
        end = start.AddHours(hours);
        return true;
    }

    private static int ParseSeat(string attendance) =>
        int.TryParse(attendance, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s > 0 ? s : 1;

    private static string SerializeLogistics(EventProposalLogisticsDto? l)
    {
        if (l == null)
            return string.Empty;
        return JsonSerializer.Serialize(l, ProposalJson.Options);
    }

    private static void ValidateStructured(Dictionary<string, List<string>> errors, ClubProposalSubmissionDto s)
    {
        if (string.IsNullOrWhiteSpace(s.PresidentStudentId))
            AddErr(errors, "presidentStudentId", "President student id is required when submitting structured officer data.");
        else if (!EightDigitStudent.IsMatch(s.PresidentStudentId.Trim()))
            AddErr(errors, "presidentStudentId", "President student id must be exactly 8 digits.");

        if (string.IsNullOrWhiteSpace(s.VicePresidentStudentId))
            AddErr(errors, "vicePresidentStudentId", "Vice president student id is required when submitting structured officer data.");
        else if (!EightDigitStudent.IsMatch(s.VicePresidentStudentId.Trim()))
            AddErr(errors, "vicePresidentStudentId", "Vice president student id must be exactly 8 digits.");

        if (!string.Equals(s.Commitment?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
            AddErr(errors, "commitment", "Commitment must be \"yes\" when submitting structured data.");

        if (HasOtherMembers(s.OtherMembersJson))
        {
            try
            {
                _ = ParseOtherMembers(s.OtherMembersJson);
            }
            catch
            {
                AddErr(errors, "otherMembersJson", "otherMembersJson must be a list of valid user ids.");
            }
        }
    }

    private static bool HasOtherMembers(IReadOnlyCollection<string>? members) =>
        members is { Count: > 0 } && members.Any(m => !string.IsNullOrWhiteSpace(m));

    private static List<OtherOfficer> ParseOtherMembers(IEnumerable<string>? members)
    {
        if (members == null)
            return new List<OtherOfficer>();

        var result = new List<OtherOfficer>();
        var seen = new HashSet<Guid>();
        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member))
                continue;

            if (!Guid.TryParse(member.Trim(), out var userId) || userId == Guid.Empty)
                throw new FormatException("otherMembersJson must contain only valid user ids.");

            if (!seen.Add(userId))
                continue;

            result.Add(new OtherOfficer
            {
                UserId = userId.ToString("D"),
                Status = "Pending"
            });
        }

        return result;
    }

    private static void ValidateLogo(Dictionary<string, List<string>> errors, string? logoFile)
    {
        if (string.IsNullOrWhiteSpace(logoFile))
            AddErr(errors, "logoFile", "Logo file is required.");
    }

    private static void ValidateConstitution(Dictionary<string, List<string>> errors, string? constitutionFile)
    {
        if (string.IsNullOrWhiteSpace(constitutionFile))
            AddErr(errors, "constitutionFile", "Constitution PDF is required.");
    }

    private static string ResolveAttachmentName(ClubProposalFilePart? filePart, string? fileReference, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(filePart?.FileName))
            return filePart.FileName.Trim();

        if (!string.IsNullOrWhiteSpace(fileReference))
        {
            var fileName = Path.GetFileName(fileReference.Trim());
            if (!string.IsNullOrWhiteSpace(fileName))
                return fileName;
        }

        return fallbackName;
    }

    private static void AddErr(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }

        list.Add(message);
    }

    private static void EnrichFromDescription(string? description, ClubProposalSubmissionDto s)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        foreach (var block in description.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (block.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                s.ShortDesc ??= block["Summary:".Length..].Trim();
            else if (block.StartsWith("Uniqueness:", StringComparison.OrdinalIgnoreCase))
                s.UniqueDesc ??= block["Uniqueness:".Length..].Trim();
            else if (block.StartsWith("Goals:", StringComparison.OrdinalIgnoreCase))
                s.Goals ??= block["Goals:".Length..].Trim();
            else if (block.StartsWith("Activities:", StringComparison.OrdinalIgnoreCase))
                s.Activities ??= block["Activities:".Length..].Trim();
            else if (block.StartsWith("Mission alignment:", StringComparison.OrdinalIgnoreCase))
                s.Alignment ??= block["Mission alignment:".Length..].Trim();
            else if (block.StartsWith("Vision:", StringComparison.OrdinalIgnoreCase))
                s.Vision ??= block["Vision:".Length..].Trim();
        }
    }

    private static string BuildDescriptionFromParts(
        string? shortDesc,
        string? uniqueDesc,
        string? goals,
        string? activities,
        string? alignment,
        string? vision)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(shortDesc))
            parts.Add($"Summary: {shortDesc}");
        if (!string.IsNullOrWhiteSpace(uniqueDesc))
            parts.Add($"Uniqueness: {uniqueDesc}");
        if (!string.IsNullOrWhiteSpace(goals))
            parts.Add($"Goals: {goals}");
        if (!string.IsNullOrWhiteSpace(activities))
            parts.Add($"Activities: {activities}");
        if (!string.IsNullOrWhiteSpace(alignment))
            parts.Add($"Mission alignment: {alignment}");
        if (!string.IsNullOrWhiteSpace(vision))
            parts.Add($"Vision: {vision}");
        return string.Join("\n\n", parts);
    }

    private static class ProposalJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
