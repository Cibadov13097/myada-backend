using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string Username { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "OrganizationalId must be exactly 9 digits.")]
        public string OrganizationalId { get; set; } = null!;

        public string? ProfileImage { get; set; }
    }
}
