namespace AuthService.Application.DTOs.Auth;

public class AdminUserListResponseDto
{
    public int Count { get; set; }
    public IReadOnlyCollection<AdminUserDto> Users { get; set; } = Array.Empty<AdminUserDto>();
}
