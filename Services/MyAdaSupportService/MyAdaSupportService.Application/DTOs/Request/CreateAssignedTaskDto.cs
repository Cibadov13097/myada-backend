namespace MyAdaSupportService.Application.DTOs.Request;

public class CreateAssignedTaskDto : CreateSupportRequestDto
{
    public Guid StaffId { get; set; }
    public string? DispatcherInstructions { get; set; }
}