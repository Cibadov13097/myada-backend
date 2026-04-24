using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Infrastructure.Repositories;

public class RequestRepository
    : EfCoreRepository<Request>, IRequestRepository
{
    public RequestRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Request?> GetByIdOrDefaultAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<Request?> GetByIdWithDetailsOrDefaultAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Request>> GetRequestsByMemberIdAsync(Guid memberId)
    {
        return await _dbSet
            .Where(r => r.CreatedUserId == memberId)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetRequestsByStaffIdAsync(Guid staffId)
    {
        return await _dbSet
            .Where(r => r.StaffId == staffId)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetOpenRequestsAsync()
    {
        return await _dbSet
            .Where(r => r.Status != RequestStatus.Completed &&
                        r.Status != RequestStatus.Cancelled)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetCompletedRequestsAsync()
    {
        return await _dbSet
            .Where(r => r.Status == RequestStatus.Completed)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetCancelledRequestsAsync()
    {
        return await _dbSet
            .Where(r => r.Status == RequestStatus.Cancelled)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetByCategoryIdAsync(int categoryId)
    {
        return await _dbSet
            .Where(r => r.CategoryId == categoryId)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetUrgentRequestsAsync()
    {
        return await _dbSet
            .Where(r => r.IsUrgent)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Request>> GetAllWithDetailsAsync(System.Linq.Expressions.Expression<Func<Request, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .AsNoTracking()
            .ToListAsync();
    }
}