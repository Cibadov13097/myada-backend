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
    }
}
