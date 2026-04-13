namespace MyAdaSupportService.Application.DTOs.Dispatcher;

public class UpdateRequestStatusDto
{
    public string Status { get; set; } // Assigned | InProgress | Completed
    public string? Note { get; set; }
}