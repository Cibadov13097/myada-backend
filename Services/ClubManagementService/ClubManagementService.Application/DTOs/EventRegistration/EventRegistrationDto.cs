namespace ClubManagementService.Application.DTOs.EventRegistration;

public class EventRegistrationDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Guid UserId { get; set; }
}

public class CreateEventRegistrationDto
{
    public int EventId { get; set; }
    public Guid UserId { get; set; }
}
