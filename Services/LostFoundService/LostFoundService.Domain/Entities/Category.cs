using LostFoundService.Domain.Common;

namespace LostFoundService.Domain.Entities
{
    public class Category : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
