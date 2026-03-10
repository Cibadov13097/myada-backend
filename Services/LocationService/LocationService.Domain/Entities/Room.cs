using LocationService.Domain.Common;
using LocationService.Domain.Enums;

namespace LocationService.Domain.Entities
{
    public class Room : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public RoomType RoomType { get; set; }
        public int BuildingId { get; set; }
        public Building Building { get; set; } = null!;
    }
}
