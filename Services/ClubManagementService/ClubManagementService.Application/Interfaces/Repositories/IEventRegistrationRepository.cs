using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IEventRegistrationRepository : IGenericRepository<EventRegistration>
{
    Task<List<EventRegistration>> GetByEventIdAsync(Guid eventId);
}
