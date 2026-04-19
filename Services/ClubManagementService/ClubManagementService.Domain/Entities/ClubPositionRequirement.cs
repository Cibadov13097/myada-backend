namespace ClubManagementService.Domain.Entities;

public class ClubPositionRequirement
{
    public int Id { get; set; }
    public int ClubPositionId { get; set; }
    public ClubPosition ClubPosition { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Text { get; set; } = null!;
}
