namespace ClubManagementService.Application.DTOs.JobVacancy;

public class JobVacancyDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid PostedBy { get; set; }
}

public class CreateJobVacancyDto
{
    public int ClubId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid PostedBy { get; set; }
}

public class UpdateJobVacancyDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
}
