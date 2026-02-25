using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Application.DTOs.JobApplication;

public class JobApplicationDto
{
    public int Id { get; set; }
    public int JobVacancyId { get; set; }
    public Guid ApplicantId { get; set; }
    public string ExperienceLinks { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public JobApplicationStatus Status { get; set; }
}

public class CreateJobApplicationDto
{
    public int JobVacancyId { get; set; }
    public Guid ApplicantId { get; set; }
    public string ExperienceLinks { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
}

public class UpdateJobApplicationStatusDto
{
    public JobApplicationStatus Status { get; set; }
}
