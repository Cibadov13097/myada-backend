namespace ClubManagementService.Application.DTOs.JobApplication;

/// <summary>Student &quot;My applications&quot; row for vacancy (job) applications.</summary>
public class MyVacancyApplicationDto
{
    public string ApplicationId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string? VacancyTitle { get; set; }
    public string? ClubName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
