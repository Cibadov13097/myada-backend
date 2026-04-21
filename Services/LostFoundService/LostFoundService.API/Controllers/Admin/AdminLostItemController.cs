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
        public async Task<IActionResult> Create([FromForm] AddLostItemDto dto)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items/admin");
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                dto.Image = uploadedImageUrl;
            }

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
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLostItemDto dto)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items/admin");
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                dto.Image = uploadedImageUrl;
            }

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

        private async Task<string?> UploadFormImageIfPresentAsync(string directory)
        {
            if (!Request.HasFormContentType)
            {
                return null;
            }

            var form = await Request.ReadFormAsync();
            var file = form.Files.GetFile("imageFile")
                ?? form.Files.GetFile("image")
                ?? form.Files.GetFile("photo")
                ?? form.Files.FirstOrDefault();

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
