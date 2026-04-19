using System.Threading;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IEventRegistrationRepository : IGenericRepository<EventRegistration>
{
    Task<List<EventRegistration>> GetByEventIdAsync(int eventId);
    Task<EventRegistration?> GetByEventAndUserAsync(int eventId, Guid userId, CancellationToken cancellationToken = default);
}
