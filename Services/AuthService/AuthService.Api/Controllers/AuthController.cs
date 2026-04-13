using AuthService.Application.DTOs.Auth;
using AuthService.Application.Interfaces;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        public AuthController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          TokenService tokenService,
          RoleManager<IdentityRole> roleManager,
          IRefreshTokenService refreshTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
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
                PhoneNumber = "1111",
                Email = "admin@gmail",
                ProfileImage = "sas",
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

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
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
                    user.PhoneNumber,
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

        [Authorize(Roles = "admin")]
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
                    user.PhoneNumber,
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                return Ok(new
                {
                    message = "If this email exists, password reset link sent.",
                    email = dto.Email,
                    token
                });
            }

            return Ok(new
            {
                message = "If this email exists, password reset link sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound("User not found");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password reset successfully");
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
                refreshToken = refreshTokenEntity.Token
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
            return Ok(new { message = "Logged out successfully" });
        }
    }
}

