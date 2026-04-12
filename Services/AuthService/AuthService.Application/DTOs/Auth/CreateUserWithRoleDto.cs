using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Auth
{
    public class CreateUserWithRoleDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = null!;
    }
}
