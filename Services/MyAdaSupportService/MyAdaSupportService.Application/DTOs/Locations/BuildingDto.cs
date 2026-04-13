namespace MyAdaSupportService.Application.DTOs.Locations;

public class BuildingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BuildingId { get; set; }
}
