using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Interfaces.Repositories;

public interface ISubeventRepository : IGenericRepository<Subevent>
{
    Task<List<Subevent>> GetByMainEventIdAsync(int mainEventId);
}
