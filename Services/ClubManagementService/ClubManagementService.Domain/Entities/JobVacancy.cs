using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class JobVacancy : AuditableEntity
    {
        public int ClubId { get; set; }
        public Club? Club { get; set; }
        /// <summary>When <see cref="ClubPositionId"/> is set, UI may leave this empty and use the position title.</summary>
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid PostedBy { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Draft vacancies are not listed publicly until published.</summary>
        public bool IsDraft { get; set; } = false;
        public int? ClubPositionId { get; set; }
        public ClubPosition? ClubPosition { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
    }
}
