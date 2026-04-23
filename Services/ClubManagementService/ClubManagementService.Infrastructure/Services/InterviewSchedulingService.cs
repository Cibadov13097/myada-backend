using System.Data;
using ClubManagementService.Application.DTOs.Interview;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Services;

public sealed class InterviewSchedulingService : IInterviewSchedulingService
{
    private readonly ClubManagementDbContext _db;

    public InterviewSchedulingService(ClubManagementDbContext db) => _db = db;

    public async Task<InterviewSlotListResult> ListSlotsAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateOnly? onDate,
        bool includeFull,
        CancellationToken cancellationToken = default)
    {
        var app = await _db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app == null)
            return new InterviewSlotListResult { NotFound = true, Message = "Interview not found." };

        if (!isAdministrator && app.ApplicantId != callerId)
            return new InterviewSlotListResult { Forbidden = true, Message = "Forbidden." };

        if (!isAdministrator && app.Status != JobApplicationStatus.InterviewInvited)
            return new InterviewSlotListResult { Forbidden = true, Message = "Slots are available after you are invited to interview." };

        var vacancy = await _db.JobVacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == app.JobVacancyId, cancellationToken)
            .ConfigureAwait(false);
        if (vacancy == null)
            return new InterviewSlotListResult { NotFound = true, Message = "Vacancy not found for interview." };

        var nowUtc = DateTimeOffset.UtcNow;

        var slotsQuery = _db.InterviewSlots
            .AsNoTracking()
            .Where(s =>
                s.ClubId == vacancy.ClubId
                && s.IsPublished
                && (s.JobVacancyId == null || s.JobVacancyId == vacancy.Id)
                && s.StartsAt > nowUtc);

        if (from is not null)
            slotsQuery = slotsQuery.Where(s => s.StartsAt >= from.Value);
        if (to is not null)
            slotsQuery = slotsQuery.Where(s => s.StartsAt <= to.Value);
        if (onDate is not null)
        {
            var dayStart = new DateTimeOffset(onDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            slotsQuery = slotsQuery.Where(s => s.StartsAt >= dayStart && s.StartsAt < dayEnd);
        }

        var slotRows = await slotsQuery
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var slotIds = slotRows.Select(s => s.SlotId).ToList();

        var takenCounts = await _db.JobApplications
            .AsNoTracking()
            .Where(a => a.InterviewSlotId != null && slotIds.Contains(a.InterviewSlotId))
            .GroupBy(a => a.InterviewSlotId!)
            .Select(g => new { SlotId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var takenMap = takenCounts.ToDictionary(x => x.SlotId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var slots = slotRows
            .Select(s =>
            {
                takenMap.TryGetValue(s.SlotId, out var bookedCount);
                var remaining = Math.Max(0, s.Capacity - bookedCount);
                var isFull = remaining <= 0;
                var bookable = !isFull;

                return new InterviewSlotItemDto
                {
                    Id = s.SlotId,
                    StartAt = s.StartsAt,
                    EndAt = s.EndsAt,
                    Timezone = "UTC",
                    Capacity = s.Capacity,
                    BookedCount = bookedCount,
                    RemainingCapacity = remaining,
                    Status = s.IsPublished ? "ACTIVE" : "CANCELLED",
                    Bookable = bookable,
                    LocationType = null,
                    MeetingUrl = null,
                    InterviewerName = null
                };
            })
            .Where(x => includeFull || x.Bookable)
            .ToList();

        return new InterviewSlotListResult { Slots = slots };
    }

    public async Task<InterviewBookingResultDto> BookSlotAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        string slotId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return new InterviewBookingResultDto { BadRequest = true, Message = "slotId is required." };

        slotId = slotId.Trim();

        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app == null)
            return new InterviewBookingResultDto { NotFound = true, Message = "Interview not found." };

        if (!isAdministrator && app.ApplicantId != callerId)
            return new InterviewBookingResultDto { Forbidden = true, Message = "Forbidden." };

        if (!isAdministrator && app.Status != JobApplicationStatus.InterviewInvited)
            return new InterviewBookingResultDto { Conflict = true, Message = "Interview slot selection is not open for this application." };

        if (!string.IsNullOrWhiteSpace(app.InterviewSlotId))
        {
            if (string.Equals(app.InterviewSlotId, slotId, StringComparison.OrdinalIgnoreCase))
            {
                var existingSlot = await _db.InterviewSlots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SlotId == slotId, cancellationToken)
                    .ConfigureAwait(false);

                return new InterviewBookingResultDto
                {
                    BookingId = app.Id.ToString(),
                    InterviewId = applicationId.ToString(),
                    SlotId = slotId,
                    Status = "CONFIRMED",
                    StartAt = existingSlot?.StartsAt,
                    EndAt = existingSlot?.EndsAt,
                    IsIdempotentReplay = true
                };
            }

            return new InterviewBookingResultDto { Conflict = true, Message = "You already have an active booking for this interview." };
        }

        var vacancy = await _db.JobVacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == app.JobVacancyId, cancellationToken)
            .ConfigureAwait(false);
        if (vacancy == null)
            return new InterviewBookingResultDto { NotFound = true, Message = "Vacancy not found for interview." };

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        try
        {
            var slot = await _db.InterviewSlots
                .Where(s => s.SlotId == slotId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (slot == null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new InterviewBookingResultDto { NotFound = true, Message = "Slot not found." };
            }

            if (!slot.IsPublished || slot.StartsAt <= DateTimeOffset.UtcNow)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new InterviewBookingResultDto { Conflict = true, Message = "Slot is not bookable." };
            }

            if (slot.ClubId != vacancy.ClubId || (slot.JobVacancyId != null && slot.JobVacancyId != vacancy.Id))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new InterviewBookingResultDto { BadRequest = true, Message = "Slot does not belong to this interview." };
            }

            var booked = await _db.JobApplications
                .Where(a => a.InterviewSlotId == slotId && !a.IsDeleted)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            if (booked >= slot.Capacity)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new InterviewBookingResultDto { Conflict = true, Message = "Slot is full or already taken." };
            }

            app.InterviewSlotId = slotId;
            app.Status = JobApplicationStatus.InterviewScheduled;
            app.ModifiedAt = DateTime.UtcNow;
            app.ModifiedByUserId = callerId;

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new InterviewBookingResultDto
            {
                BookingId = app.Id.ToString(),
                InterviewId = applicationId.ToString(),
                SlotId = slotId,
                Status = "CONFIRMED",
                StartAt = slot.StartsAt,
                EndAt = slot.EndsAt
            };
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new InterviewBookingResultDto { Conflict = true, Message = "Slot is full or already taken." };
        }
    }

    public async Task<InterviewBookingResultDto> GetBookingAsync(
        int applicationId,
        Guid callerId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var app = await _db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app == null)
            return new InterviewBookingResultDto { NotFound = true, Message = "Interview not found." };

        if (!isAdministrator && app.ApplicantId != callerId)
            return new InterviewBookingResultDto { Forbidden = true, Message = "Forbidden." };

        if (string.IsNullOrWhiteSpace(app.InterviewSlotId))
            return new InterviewBookingResultDto { NotFound = true, Message = "No booking found." };

        var slot = await _db.InterviewSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SlotId == app.InterviewSlotId, cancellationToken)
            .ConfigureAwait(false);

        return new InterviewBookingResultDto
        {
            BookingId = app.Id.ToString(),
            InterviewId = applicationId.ToString(),
            SlotId = app.InterviewSlotId!,
            Status = "CONFIRMED",
            StartAt = slot?.StartsAt,
            EndAt = slot?.EndsAt
        };
    }
}
