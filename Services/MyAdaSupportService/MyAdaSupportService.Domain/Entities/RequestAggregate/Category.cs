using MyAdaSupportService.Domain.Entities.BaseAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Domain.Entities.RequestAggregate;

public class Category : BaseAuditedEntity
{
    public SupportArea Module { get; set; }
    public string Name { get; set; }
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}