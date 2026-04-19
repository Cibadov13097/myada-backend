using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface IEventRepository : IGenericRepository<Event>
{
    Task<List<Event>> GetByClubIdAsync(int clubId);
}
