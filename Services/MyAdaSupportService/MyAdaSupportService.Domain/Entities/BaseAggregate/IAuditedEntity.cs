namespace MyAdaSupportService.Domain.Entities.BaseAggregate;

public interface IAuditedEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid CreatedUserId { get; set; }
    public Guid? ModifiedUserId { get; set; }

    public void SetCreated(Guid userId)
    {
        CreatedUserId = userId;
        CreatedAt = DateTime.UtcNow;
        ModifiedUserId = null;
        ModifiedAt = null;
    }

    public void SetModified(Guid userId)
    {
        ModifiedUserId = userId;
        ModifiedAt = DateTime.UtcNow;
    }
}

// public interface IAuditedEntity : IAuditedEntity where TUser : BaseEntity
// {
//     public int CreatedUserId { get; set; }
//     public int? ModifiedUserId { get; set; }
//     public TUser CreatedUser { get; set; }
//     public TUser? ModifiedUser { get; set; }

//     public void SetCreated(int userId)
//     {
//         CreatedUserId = userId;
//         CreatedAt = DateTime.Now;
//         ModifiedUserId = null;
//         ModifiedAt = null;
//     }

//     public void SetModified(int userId)
//     {
//         ModifiedUserId = userId;
//         ModifiedAt = DateTime.Now;
//     }
// }
