using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.Extensions;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Api.Models.LostItem.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFoundService.Api.Controllers
{
    [Route("api/v1/lost-items")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Lost Items")]

    public class LostItemController : ControllerBase
    {
        private readonly ILostItemService _service;
        private readonly IImageStorageService _imageStorageService;

        public LostItemController(
            ILostItemService service,
            IImageStorageService imageStorageService)
        {
            _service = service;
            _imageStorageService = imageStorageService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllLostItemsAsync());


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _service.GetLostItemByIdAsync(id));


        [Authorize]
        [HttpGet("users/me")]
        public async Task<IActionResult> GetMy() => Ok(await _service.GetMyLostItemsAsync(User.GetUserId()));


        [Authorize]
        [HttpGet("users/me/{id}")]
        public async Task<IActionResult> GetMyById(int id) => Ok(await _service.GetMyLostItemByIdAsync(id, User.GetUserId()));


        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateLostItemFormRequest request)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items", request.ImageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "imageFile is required." });
            }

            var dto = new AddLostItemDto
            {
                Title = request.Title,
                Description = request.Description,
                BuildingId = request.BuildingId,
                RoomId = request.RoomId,
                CategoryId = request.CategoryId,
                Image = uploadedImageUrl
            };
            var created = await _service.AddLostItemAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }


        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLostItemFormRequest request)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/lost-items", request.ImageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            var dto = new UpdateLostItemDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                BuildingId = request.BuildingId,
                RoomId = request.RoomId,
                CategoryId = request.CategoryId,
                Image = string.IsNullOrWhiteSpace(uploadedImageUrl) ? null : uploadedImageUrl
            };

            // Ignore manual URL updates; image changes must come from uploaded file.
            return Ok(await _service.UpdateLostItemAsync(id, dto, User.GetUserId()));
        }


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteLostItemAsync(id, User.GetUserId());
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
