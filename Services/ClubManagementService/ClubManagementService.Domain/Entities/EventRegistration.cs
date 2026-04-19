using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class EventRegistration : AuditableEntity
    {
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public Guid UserId { get; set; }
    }
}
