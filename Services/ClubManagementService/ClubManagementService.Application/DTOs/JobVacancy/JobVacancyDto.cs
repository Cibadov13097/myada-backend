namespace ClubManagementService.Application.DTOs.JobVacancy;

public class JobVacancyDto
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid PostedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDraft { get; set; }
    public int? ClubPositionId { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    /// <summary>Lifecycle: "draft" | "active" | "inactive".</summary>
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
}

public class CreateJobVacancyDto
{
    public int ClubId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid PostedBy { get; set; }
    public int? ClubPositionId { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    /// <summary>When false, vacancy is saved as draft (not listed publicly).</summary>
    public bool Publish { get; set; } = true;
}

public class UpdateJobVacancyDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? ClubPositionId { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public bool? IsDraft { get; set; }
}
