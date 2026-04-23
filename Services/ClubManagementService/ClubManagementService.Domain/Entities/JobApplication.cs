using System;
using ClubManagementService.Domain.Common;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Domain.Entities
{
    public class JobApplication : AuditableEntity
    {
        public int JobVacancyId { get; set; }
        public JobVacancy? JobVacancy { get; set; }
        public Guid ApplicantId { get; set; }
        public string ExperienceLinks { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public JobApplicationStatus Status { get; set; }

        /// <summary>References <see cref="InterviewSlot.SlotId"/> when the applicant has booked a slot.</summary>
        public string? InterviewSlotId { get; set; }
        public InterviewSlot? InterviewSlot { get; set; }
    }
}
