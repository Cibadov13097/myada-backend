using MyAdaSupportService.Domain.Entities.BaseAggregate;

namespace MyAdaSupportService.Domain.Entities.RequestAggregate;

public class Attachment : BaseAuditedEntity
{
    public int RequestId { get; set; }
    public string Url { get; set; } = string.Empty;
    public Request? Request { get; set; }
}