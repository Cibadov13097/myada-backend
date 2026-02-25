using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class EventRegistration : AuditableEntity
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
    }
}
