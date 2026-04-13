namespace MyAdaSupportService.Application.DTOs.Staff;

public class SupportStaffListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Specialization { get; set; }
    public string Area { get; set; }       // IT | FM
    public string Status { get; set; }     // Online | Offline | OnBreak
    public int ActiveTasks { get; set; }
    public int LoadPercent { get; set; }
}
