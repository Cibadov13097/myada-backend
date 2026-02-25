namespace ClubManagementService.Application.DTOs.EventRegistration;

public class EventRegistrationDto
{
    public int Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}

public class CreateEventRegistrationDto
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}
