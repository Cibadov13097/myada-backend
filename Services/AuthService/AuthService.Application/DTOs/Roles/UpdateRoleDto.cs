using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Roles;

public class UpdateRoleDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = null!;
}
