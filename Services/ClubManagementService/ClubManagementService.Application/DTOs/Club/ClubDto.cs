using ClubManagementService.Domain.Enums;
using System.Text.Json.Serialization;

namespace ClubManagementService.Application.DTOs.Club;

public class ClubDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
    public ClubStatus Status { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<ClubFocusAreaItemDto> FocusAreas { get; set; } = new();
    public ClubSocialLinksDto? SocialLinks { get; set; }
    public string? Email { get; set; }
    public List<ClubResourceItemDto> Resources { get; set; } = new();

    /// <summary>Approved logo/profile image URL (relative or absolute).</summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>Pending replacement logo; student services can approve.</summary>
    public string? ProposedProfileImageUrl { get; set; }

    /// <summary>Same as <see cref="Resources"/>; included for clients that read <c>documents</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ClubResourceItemDto>? Documents => Resources is { Count: > 0 } ? Resources : null;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Website => NonEmpty(SocialLinks?.Website);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Instagram => NonEmpty(SocialLinks?.Instagram);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? X => NonEmpty(SocialLinks?.X);

    /// <summary>Legacy alias for <see cref="X"/>; same canonical value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Twitter => NonEmpty(SocialLinks?.X);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tiktok => NonEmpty(SocialLinks?.Tiktok);

    private static string? NonEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

public class CreateClubDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid ApprovedBy { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateClubDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public ClubStatus Status { get; set; }
}
