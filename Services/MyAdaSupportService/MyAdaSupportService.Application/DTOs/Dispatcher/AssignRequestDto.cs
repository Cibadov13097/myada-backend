namespace MyAdaSupportService.Application.DTOs.Dispatcher;

public class AssignRequestDto
{
    public Guid StaffId { get; set; }
    public string? DispatcherInstructions { get; set; }
}
