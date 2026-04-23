using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities;

/// <summary>Persisted interview time slot for a club; optional <see cref="JobVacancyId"/> scopes the slot to one vacancy, or null for any vacancy.</summary>
public class InterviewSlot : AuditableEntity
{
    /// <summary>Stable external id (GUID string) used in APIs and <see cref="JobApplication.InterviewSlotId"/>.</summary>
    public string SlotId { get; set; } = null!;

    public int ClubId { get; set; }
    public Club? Club { get; set; }

    public int? JobVacancyId { get; set; }
    public JobVacancy? JobVacancy { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public int Capacity { get; set; } = 1;
    public bool IsPublished { get; set; } = true;
}
