using AuthService.Domain.Enums;

namespace AuthService.Application.DTOs.Auth;

public class AdminUserDto
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string OrganizationalId { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfileImage { get; set; }
    public UserType UserType { get; set; }
    public Status Status { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
