namespace ClubManagementService.Application.DTOs.Subevent;

public class SubeventDto
{
    public int Id { get; set; }
    public int MainEventId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class CreateSubeventDto
{
    public int MainEventId { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class UpdateSubeventDto
{
    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
