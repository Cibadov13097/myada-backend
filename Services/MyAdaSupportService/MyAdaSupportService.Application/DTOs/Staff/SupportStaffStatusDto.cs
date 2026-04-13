namespace MyAdaSupportService.Application.DTOs.Staff;

public class SupportStaffStatusDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }

    public string Status { get; set; } = null!;
    // Online | Offline | OnBreak

    public DateTime? UpdatedAt { get; set; }       // optional but recommended
}