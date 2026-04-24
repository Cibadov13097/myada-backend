using MyAdaSupportService.Application.DTOs.Request;

namespace MyAdaSupportService.Application.DTOs.Staff;

public class StaffWorkloadDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; }
    public string Area { get; set; }
    public string Status { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedToday { get; set; }
    public int LoadPercent { get; set; }
    public List<SupportRequestListItemDto> ActiveRequests { get; set; } = new();
}