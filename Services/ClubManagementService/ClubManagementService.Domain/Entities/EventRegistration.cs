using System;
using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class EventRegistration : AuditableEntity
    {
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public Guid UserId { get; set; }

        // Ticket / QR
        public string TicketId { get; set; } = null!;
        public string RegistrationJwt { get; set; } = null!;

        // Event snapshot for stable "My Tickets" cards
        public string EventNameSnapshot { get; set; } = null!;
        public string? EventImageUrlSnapshot { get; set; }
        public DateTime EventStartTimeSnapshot { get; set; }
        public DateTime EventEndTimeSnapshot { get; set; }
        public string EventLocationSnapshot { get; set; } = null!;
        public int EventSeatLimitSnapshot { get; set; }

        // Check-in fields
        public bool IsCheckedIn { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public Guid? CheckedInBy { get; set; }
        public int ScanCount { get; set; }
        public DateTime? LastScannedAt { get; set; }
    }
}
