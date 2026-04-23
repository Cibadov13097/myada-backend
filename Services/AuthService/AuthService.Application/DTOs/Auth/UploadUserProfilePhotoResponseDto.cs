namespace AuthService.Application.DTOs.Auth
{
    public class UploadUserProfilePhotoResponseDto
    {
        public string UserId { get; set; } = null!;
        public string OrganizationalId { get; set; } = null!;
        public string ProfileImage { get; set; } = null!;
    }
}
