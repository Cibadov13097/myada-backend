namespace MyAdaSupportService.Application.DTOs.Request;

public class UpdateSupportRequestDto
{
    public int CategoryId { get; set; }

    public string Description { get; set; } = null!;

    public bool IsBuilding { get; set; }
    public int? BuildingId { get; set; }

    public bool IsRoom { get; set; }
    public int? RoomId { get; set; }

    public string? AreaDetails { get; set; }

    public bool IsUrgent { get; set; }
}
