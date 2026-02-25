using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Application.DTOs.Club;

public class ClubDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
    public ClubStatus Status { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

public class CreateClubDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateClubDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public ClubStatus Status { get; set; }
}
