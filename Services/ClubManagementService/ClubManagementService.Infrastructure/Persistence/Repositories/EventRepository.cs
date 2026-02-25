using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;

namespace ClubManagementService.Infrastructure.Persistence.Repositories;

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(ClubManagementDbContext dbContext) : base(dbContext)
    {
    }
}
