using ClubManagementService.Domain.Common;

namespace ClubManagementService.Domain.Entities
{
    public class ClubAnnouncement : AuditableEntity
    {
        public int ClubId { get; set; }
        public Club Club { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
