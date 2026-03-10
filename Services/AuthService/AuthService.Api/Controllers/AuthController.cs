using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

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

        public AuthController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          TokenService tokenService,
          RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
        }

        [HttpGet("create-admin")]

        public async Task<IActionResult> CreateAdmin()
        {
            if (!await _roleManager.RoleExistsAsync("admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("admin"));
            }
            ApplicationUser user = new ApplicationUser
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
            var result = await _userManager.AddToRoleAsync(user, "admin");
            if (result.Succeeded)
            {
                Ok();
            }

            return Ok();
        }



        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgetPasswordDto dto)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(dto.Email);


            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                return Ok(new
                {
                    message = "If this email exists, password reset link sent.",
                    email = dto.Email,
                    token = token
                });
            }


            return Ok(
                new
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

            var result = await _userManager.ResetPasswordAsync(
                user,
                dto.Token,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password reset successfully");
        }


        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password Changed succesfully");

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


            var token = await _tokenService.GenerateToken(user);

            return Ok(new { accessToken = token });

        }

  
    }
}
