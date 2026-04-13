using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Infrastructure.Repositories;

public class AttachmentRepository
    : EfCoreRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Attachment>> GetByRequestIdAsync(int requestId)
    {
        return await _dbSet
            .Where(a => a.RequestId == requestId)
            .AsNoTracking()
            .ToListAsync();
    }
}