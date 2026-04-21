using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.Extensions;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFoundService.Api.Controllers.Admin
{
    [Route("api/v1/admin/lost-items")]
    [ApiController]
    [Tags("Lost Items - Admin")]

    public class AdminLostItemController : ControllerBase
    {
        private readonly ILostItemAdminService _service;
        private readonly IImageStorageService _imageStorageService;

        public AdminLostItemController(
            ILostItemAdminService lostItemAdminService,
            IImageStorageService imageStorageService)
        {
            _service = lostItemAdminService;
            _imageStorageService = imageStorageService;
        }

        // ===============================
        // GET ALL (ADMIN)
        // ===============================
        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpGet]
        public async Task<IActionResult> GetLostItems([FromQuery] bool includeDeleted = false)
        {
            var resultDto = await _service.AdminGetAllAsync(includeDeleted);
            return Ok(resultDto);
        }

        // ===============================
        // GET BY ID (ADMIN)
        // ===============================
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLostItemById(int id)
        {
            var resultDto = await _service.AdminGetByIdAsync(id);
            return Ok(resultDto);
        }

        // ===============================
        // CREATE (ADMIN)
        // ===============================
        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AddLostItemDto dto, IFormFile? imageFile)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items/admin", imageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "imageFile is required." });
            }

            dto.Image = uploadedImageUrl;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var adminId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(adminId))
                throw new UnauthorizedException("Admin not found!");

            var resultDto = await _service.AdminAddLostItemAsync(dto, adminId);
            return Ok(resultDto);
        }

        // ===============================
        // UPDATE (ADMIN)
        // ===============================
        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLostItemDto dto, IFormFile? imageFile)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items/admin", imageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            // Ignore manual URL updates; image changes must come from uploaded file.
            dto.Image = string.IsNullOrWhiteSpace(uploadedImageUrl) ? null : uploadedImageUrl;

            var adminId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(adminId))
                throw new UnauthorizedException("Admin not authenticated.");

            var resultDto = await _service.AdminUpdateAsync(id, dto, adminId);
            return Ok(resultDto);
        }

        // ===============================
        // SOFT DELETE (ADMIN)
        // ===============================
        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var adminId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(adminId))
                throw new UnauthorizedException("Admin not authenticated.");

            await _service.AdminDeleteAsync(id, adminId);
            return NoContent();
        }

        // ===============================
        // FORCE DELETE (ADMIN)
        // ===============================
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}/force")]
        public async Task<IActionResult> ForceDelete(int id)
        {
            await _service.AdminForceDeleteAsync(id);
            return NoContent();
        }

        private async Task<string?> UploadFormImageIfPresentAsync(string directory, IFormFile? imageFile)
        {
            var file = imageFile;
            if (file == null && Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                file = form.Files.GetFile("imageFile")
                    ?? form.Files.GetFile("image")
                    ?? form.Files.GetFile("photo")
                    ?? form.Files.FirstOrDefault();
            }

            if (file == null || file.Length == 0)
            {
                return null;
            }

            if (!_imageStorageService.IsSupportedImage(file.FileName))
            {
                return string.Empty;
            }

            await using var stream = file.OpenReadStream();
            return await _imageStorageService.UploadImageAsync(
                stream,
                file.FileName,
                file.ContentType,
                directory,
                HttpContext.RequestAborted);
        }
    }
}
