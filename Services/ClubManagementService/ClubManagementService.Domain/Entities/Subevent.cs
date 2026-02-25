using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class Subevent : AuditableEntity
    {
        public int MainEventId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
