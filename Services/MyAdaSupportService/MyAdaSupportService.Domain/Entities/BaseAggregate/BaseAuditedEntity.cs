namespace MyAdaSupportService.Domain.Entities.BaseAggregate;

public abstract class BaseAuditedEntity : BaseEntity, IAuditedEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid CreatedUserId { get; set; }
    public Guid? ModifiedUserId { get; set; }

    // public void ClearAudit()
    // {
    //     CreatedAt = default;
    //     CreatedUserId = default;
    //     ModifiedAt = null;
    //     ModifiedUserId = default;
    // }
    public void SetCreated(Guid userId)
    {
        CreatedUserId = userId;
        CreatedAt = DateTime.UtcNow;
        ModifiedUserId = null;
        ModifiedAt = null;
    }
}