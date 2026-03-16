using LocationService.Domain.Common;

namespace LocationService.Domain.Entities
{
    public class Building : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
 
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
