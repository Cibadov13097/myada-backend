using AuthService.Application.DTOs.Auth;
using AuthService.Application.Interfaces;
using AuthService.API.DTOs.Auth;
using AuthService.Domain.Enums;
using AuthService.API.Integrations;
using AuthService.API.Security;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IProfileImageStorageService _profileImageStorageService;
        private readonly SupportStaffProvisioningClient _supportStaffProvisioningClient;
        private readonly NotificationEmailClient _notificationEmailClient;
        private readonly IAccessTokenRevocationStore _accessTokenRevocationStore;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          TokenService tokenService,
          RoleManager<IdentityRole> roleManager,
          IRefreshTokenService refreshTokenService,
          IProfileImageStorageService profileImageStorageService,
          SupportStaffProvisioningClient supportStaffProvisioningClient,
          NotificationEmailClient notificationEmailClient,
          IAccessTokenRevocationStore accessTokenRevocationStore,
          ILogger<AuthController> logger,
          IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
            _profileImageStorageService = profileImageStorageService;
            _supportStaffProvisioningClient = supportStaffProvisioningClient;
            _notificationEmailClient = notificationEmailClient;
            _accessTokenRevocationStore = accessTokenRevocationStore;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("create-admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            if (!await _roleManager.RoleExistsAsync("admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("admin"));
            }

            var user = new ApplicationUser
            {
                UserName = "Admin",
                FirstName = "admin",
                LastName = "admin",
                OrganizationalId = "900000001",
                PhoneNumber = "1111",
                Email = "admin@gmail",
                ProfileImage = null,
                CreatedAt = DateTime.UtcNow,
            };

            await _userManager.CreateAsync(user, "Admin123*");
            await _userManager.AddToRoleAsync(user, "admin");

            return Ok();
        }


        [Authorize(Roles = "admin")]
        [HttpPost("create-user-with-role")]
        public async Task<IActionResult> CreateUserWithRole(CreateUserWithRoleDto dto)
        {
            var roleName = dto.Role.Trim().ToLowerInvariant();

            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest($"Role '{dto.Role}' does not exist");

            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null)
                return BadRequest("Email is already in use");

            var existingUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUsername != null)
                return BadRequest("Username is already in use");

            var organizationalIdInUse = await _userManager.Users
                .AnyAsync(x => x.OrganizationalId == dto.OrganizationalId);
            if (organizationalIdInUse)
                return BadRequest("OrganizationalId is already in use");

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                OrganizationalId = dto.OrganizationalId,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                ProfileImage = dto.ProfileImage,
                UserType = MapUserType(roleName),
                Status = Status.Active,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                return BadRequest(createResult.Errors);

            var addToRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addToRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(addToRoleResult.Errors);
            }

            var supportProvisionResult = await _supportStaffProvisioningClient
                .ProvisionForRoleAsync(user.Id, roleName, HttpContext.RequestAborted);

            if (!supportProvisionResult.IsSuccess)
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
                await _userManager.DeleteAsync(user);

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = supportProvisionResult.ErrorMessage ?? "Failed to provision support staff status."
                });
            }

            return Ok(new
            {
                message = "User created successfully",
                user = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.OrganizationalId,
                    user.PhoneNumber,
                    user.ProfileImage,
                    role = roleName
                }
            });
        }

        private static UserType MapUserType(string roleName)
        {
            return roleName switch
            {
                "student" => UserType.Student,
                "instructor" => UserType.Faculty,
                "dispatcher" => UserType.Staff,
                _ => UserType.Staff
            };
        }

        [Authorize]
        [HttpGet("users-by-role/{role}")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return BadRequest("Role is required");

            var roleName = role.Trim().ToLowerInvariant();

            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest($"Role '{role}' does not exist");

            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

            var users = usersInRole
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.OrganizationalId,
                    user.PhoneNumber,
                    user.ProfileImage,
                    user.UserType,
                    user.Status
                })
                .ToList();

            return Ok(new
            {
                role = roleName,
                count = users.Count,
                users
            });
        }

        [Authorize]
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User id is required");

            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

            //var isAdmin = User.IsInRole("admin");
            //var isSelf = !string.IsNullOrWhiteSpace(requesterId) &&
            //             string.Equals(requesterId, id, StringComparison.Ordinal);

            //if (!isAdmin && !isSelf)
            //    return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.OrganizationalId,
                user.PhoneNumber,
                user.ProfileImage,
                user.UserType,
                user.Status,
                roles
            });
        }

        [Authorize]
        [HttpGet("users/by-organizational-id/{organizationalId}")]
        public async Task<IActionResult> GetUserByOrganizationalId(string organizationalId)
        {
            if (string.IsNullOrWhiteSpace(organizationalId))
                return BadRequest("OrganizationalId is required");

            var normalizedOrganizationalId = organizationalId.Trim();
            var requesterOrganizationalId = User.FindFirstValue("organizationalId");
            var isAdmin = User.IsInRole("admin");
            var isSelf = !string.IsNullOrWhiteSpace(requesterOrganizationalId) &&
                         string.Equals(requesterOrganizationalId, normalizedOrganizationalId, StringComparison.Ordinal);

            if (!isAdmin && !isSelf)
                return Forbid();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(item => item.OrganizationalId == normalizedOrganizationalId);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.OrganizationalId,
                user.PhoneNumber,
                user.ProfileImage,
                user.UserType,
                user.Status,
                roles
            });
        }

        [Authorize]
        [HttpPost("users/{id}/profile-photo")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UploadUserProfilePhotoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> UploadUserProfilePhoto(string id, [FromForm] UploadUserProfilePhotoDto dto)
        {
            return await SaveUserProfilePhotoAsync(id, dto);
        }

        [Authorize]
        [HttpPatch("users/{id}/profile-photo")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UploadUserProfilePhotoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> UpdateUserProfilePhoto(string id, [FromForm] UploadUserProfilePhotoDto dto)
        {
            return await SaveUserProfilePhotoAsync(id, dto);
        }

        private async Task<IActionResult> SaveUserProfilePhotoAsync(string id, UploadUserProfilePhotoDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User id is required");

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("Profile photo file is required");

            if (!_profileImageStorageService.IsSupportedImage(dto.File.FileName))
                return BadRequest("Only PNG, JPG, JPEG, and WEBP files are supported");

            const long maxFileSizeBytes = 5 * 1024 * 1024;
            if (dto.File.Length > maxFileSizeBytes)
                return BadRequest("Profile photo must be 5 MB or smaller");

            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");
            var isAdmin = User.IsInRole("admin");
            var isSelf = !string.IsNullOrWhiteSpace(requesterId) &&
                         string.Equals(requesterId, id, StringComparison.Ordinal);

            if (!isAdmin && !isSelf)
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found");

            string profileImageUrl;
            try
            {
                await using var stream = dto.File.OpenReadStream();
                profileImageUrl = await _profileImageStorageService.UploadProfileImageAsync(
                    stream,
                    dto.File.FileName,
                    dto.File.ContentType,
                    user.Id,
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to upload profile photo for user {UserId}. Verify Auth service S3 bucket credentials and bucket access.",
                    user.Id);

                return Problem(
                    title: "Profile photo upload failed",
                    detail: "The Auth service could not store the profile photo in S3. Verify the Auth service S3 bucket credentials and bucket access.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            user.ProfileImage = profileImageUrl;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(updateResult.Errors);

            return Ok(new UploadUserProfilePhotoResponseDto
            {
                UserId = user.Id,
                OrganizationalId = user.OrganizationalId,
                ProfileImage = profileImageUrl
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Ok(new
                {
                    message = "If this email exists, password reset link sent."
                });
            }

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var subject = "MyADA Password Reset";
                var recipientEmail = string.IsNullOrWhiteSpace(user.Email) ? dto.Email.Trim() : user.Email;
                var resetLink = BuildPasswordResetLink(recipientEmail!, token);
                var body = BuildPasswordResetMessage(user.FirstName, resetLink);

                var sent = await _notificationEmailClient.SendEmailAsync(
                    recipientEmail!,
                    subject,
                    body,
                    Guid.TryParse(user.Id, out var notificationRecipientUserId) ? notificationRecipientUserId : null,
                    HttpContext.RequestAborted);

                if (!sent)
                {
                    _logger.LogWarning(
                        "Failed to dispatch forgot-password email via NotificationService for user {UserId}.",
                        user.Id);
                }

                return Ok(new
                {
                    message = "If this email exists, password reset link sent."
                });
            }

            return Ok(new
            {
                message = "If this email exists, password reset link sent."
            });
        }

        private string BuildPasswordResetLink(string email, string token)
        {
            var resetPageUrl = _configuration["PasswordReset:ClientResetUrl"];
            if (string.IsNullOrWhiteSpace(resetPageUrl))
                resetPageUrl = "http://localhost:3000/reset-password";

            var separator = resetPageUrl.Contains('?') ? "&" : "?";
            var encodedEmail = Uri.EscapeDataString(email);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            return $"{resetPageUrl}{separator}email={encodedEmail}&token={encodedToken}";
        }

        private static string BuildPasswordResetMessage(string? firstName, string resetLink)
        {
            var name = string.IsNullOrWhiteSpace(firstName) ? "User" : firstName.Trim();
            var sb = new StringBuilder();
            sb.AppendLine($"Hello {name},");
            sb.AppendLine();
            sb.AppendLine("We received a request to reset your MyADA password.");
            sb.AppendLine("Use the following link to reset your password:");
            sb.AppendLine();
            sb.AppendLine(resetLink);
            sb.AppendLine();
            sb.AppendLine("If you did not request this, please ignore this email.");
            return sb.ToString();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var email = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found");

            var token = NormalizeResetToken(dto.Token);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password reset successfully");
        }

        private static string NormalizeResetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            var normalized = token.Trim();

            if (normalized.Contains('%'))
                normalized = Uri.UnescapeDataString(normalized);

            // Some clients decode query params and turn '+' into spaces; Identity expects '+'.
            if (normalized.Contains(' '))
                normalized = normalized.Replace(" ", "+");

            try
            {
                var bytes = WebEncoders.Base64UrlDecode(normalized);
                var decoded = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrWhiteSpace(decoded))
                    return decoded;
            }
            catch (FormatException)
            {
                // Legacy plain token format; keep normalized value.
            }

            return normalized;
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password Changed successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);

            if (user == null)
                return Unauthorized("Username or password is incorrect");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

            if (!result.Succeeded)
                return Unauthorized("Username or password is incorrect");

            var accessToken = await _tokenService.GenerateToken(user);
            var refreshTokenEntity = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

            return Ok(new
            {
                accessToken,
                refreshToken = refreshTokenEntity.Token,
                profileImage = user.ProfileImage,
                organizationalId = user.OrganizationalId
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto dto)
        {
            var storedToken = await _refreshTokenService.GetByTokenAsync(dto.RefreshToken);

            if (storedToken == null)
                return Unauthorized("Invalid refresh token");

            if (storedToken.IsExpired)
                return Unauthorized("Refresh token expired");

            if (storedToken.IsRevoked)
                return Unauthorized("Refresh token revoked");

            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if (user == null)
                return Unauthorized();

            var newAccessToken = await _tokenService.GenerateToken(user);
            var newRefreshTokenEntity = await _refreshTokenService.RotateRefreshTokenAsync(dto.RefreshToken);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshTokenEntity.Token
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(dto.RefreshToken);

            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                      ?? User.FindFirstValue("jti");
            var expRaw = User.FindFirstValue(JwtRegisteredClaimNames.Exp)
                         ?? User.FindFirstValue("exp");

            if (!string.IsNullOrWhiteSpace(jti) && long.TryParse(expRaw, out var expUnixSeconds))
            {
                try
                {
                    var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds).UtcDateTime;
                    await _accessTokenRevocationStore.RevokeAsync(jti, expiresAtUtc, HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to revoke access token JTI {Jti} in Redis during logout.", jti);
                }
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}

