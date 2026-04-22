using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Auth
{
    public class UploadUserProfilePhotoDto
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
