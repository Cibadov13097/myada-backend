namespace MyAdaSupportService.Domain.Entities.BaseAggregate;

public abstract class BaseAuditedEntity : BaseEntity, IAuditedEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int CreatedUserId { get; set; }
    public int? ModifiedUserId { get; set; }

    // public void ClearAudit()
    // {
    //     CreatedAt = default;
    //     CreatedUserId = default;
    //     ModifiedAt = null;
    //     ModifiedUserId = default;
    // }
    public void SetCreated(int userId)
    {
        CreatedUserId = userId;
        CreatedAt = DateTime.UtcNow;
        ModifiedUserId = null;
        ModifiedAt = null;
    }
}