using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities;

public sealed class InterviewSlot : AuditableEntity
{
    /// <summary>
    /// Public stable id used by frontend (string GUID).
    /// DB primary key remains <see cref="BaseEntity.Id"/> (int).
    /// </summary>
    public string SlotId { get; set; } = null!;

    public int ClubId { get; set; }
    public int? JobVacancyId { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>How many applications can reserve this slot. Student flow assumes 1.</summary>
    public int Capacity { get; set; } = 1;

    /// <summary>Only published slots are visible to students.</summary>
    public bool IsPublished { get; set; } = true;
}

