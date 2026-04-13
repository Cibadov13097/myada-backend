using MyAdaSupportService.Domain.Entities.RequestAggregate;
using System.Linq.Expressions;

namespace MyAdaSupportService.Application.Abstractions;

public interface IRequestRepository : IRepository<Request>
{
    Task<Request?> GetByIdOrDefaultAsync(int id);

    Task<List<Request>> GetRequestsByMemberIdAsync(int memberId);
    Task<List<Request>> GetRequestsByStaffIdAsync(int staffId);
    Task<List<Request>> GetOpenRequestsAsync();
    Task<List<Request>> GetCompletedRequestsAsync();
    Task<List<Request>> GetCancelledRequestsAsync();
    Task<List<Request>> GetByCategoryIdAsync(int categoryId);
    Task<List<Request>> GetUrgentRequestsAsync();
    Task<Request?> GetByIdWithDetailsOrDefaultAsync(int id);
    Task<List<Request>> GetAllWithDetailsAsync(Expression<Func<Request, bool>> predicate);
}
