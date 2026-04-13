namespace MyAdaSupportService.Application.DTOs.Request;

public class SupportRequestCreatedDto
{
    public int Id { get; set; }
    public string TicketNo { get; set; }          // T-1234
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}