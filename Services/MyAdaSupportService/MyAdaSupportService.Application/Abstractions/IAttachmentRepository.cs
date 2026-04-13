using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.Abstractions;

public interface IAttachmentRepository : IRepository<Attachment>
{
    Task<List<Attachment>> GetByRequestIdAsync(int requestId);
}
