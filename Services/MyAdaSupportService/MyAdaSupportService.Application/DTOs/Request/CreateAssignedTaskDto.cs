namespace MyAdaSupportService.Application.DTOs.Request;

public class CreateAssignedTaskDto : CreateSupportRequestDto
{
    public int StaffId { get; set; }
    public string? DispatcherInstructions { get; set; }
}