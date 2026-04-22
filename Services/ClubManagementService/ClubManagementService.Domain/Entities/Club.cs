using System;
using ClubManagementService.Domain.Common;
using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Domain.Entities
{
    public class Club : AuditableEntity
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid ApprovedBy { get; set; }
        public ClubStatus Status { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        /// <summary>JSON array of focus area objects; replace-only semantics on PATCH.</summary>
        public string? FocusAreasJson { get; set; }

        /// <summary>JSON object for social links (website, instagram, x, tiktok).</summary>
        public string? SocialLinksJson { get; set; }

        public string? Email { get; set; }

        /// <summary>JSON array of downloadable resources for the public club page.</summary>
        public string? ResourcesJson { get; set; }

        /// <summary>Approved logo/profile image URL (relative or absolute).</summary>
        public string? ProfileImageUrl { get; set; }

        /// <summary>Pending replacement logo; only visible to student services until approved.</summary>
        public string? ProposedProfileImageUrl { get; set; }
    }
}
