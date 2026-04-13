namespace MyAdaSupportService.Application.DTOs.Staff;

public class UpdateSupportStaffStatusDto
{
    public string Status { get; set; } = null!;
    // Online | Offline | OnBreak
}