namespace MyAdaSupportService.Application.DTOs.Staff;

public class CreateSupportStaffStatusDto
{
    public int MemberId { get; set; }

    public string Status { get; set; } = null!;
    // Online | Offline | OnBreak
}