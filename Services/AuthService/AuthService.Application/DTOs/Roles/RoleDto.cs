namespace AuthService.Application.DTOs.Roles;

public class RoleDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? NormalizedName { get; set; }
    public int UserCount { get; set; }
}
