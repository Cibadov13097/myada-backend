using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.Extensions;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;
using LostFoundService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LostFoundService.Api.Controllers
{
    [Route("api/v1/found-items")]
    [ApiController]
    [Tags("Found Items")]

    public class FoundItemController : ControllerBase
    {
        private readonly IFoundItemService _service;
        private readonly IMapper _mapper;
        private readonly IImageStorageService _imageStorageService;

        public FoundItemController(
            IFoundItemService foundItemService,
            IMapper mapper,
            IImageStorageService imageStorageService
            )
        {
            _mapper = mapper;
            _service = foundItemService;
            _imageStorageService = imageStorageService;
        }


        [HttpGet]
        public async Task<IActionResult> GetFoundItems()
        {
            var resultDto = await _service.GetAllFoundItemsAsync();

          

            return Ok(resultDto);
        }


        [Authorize]
        [HttpGet("users/me")]
        public async Task<IActionResult> GetMyFoundItems()
        {
            var userId = User.GetUserId();
            if (userId == null)
            {
                throw new UnauthorizedException("User is not authenticated");
            }

            var resultDto = await _service.GetMyFoundItemsAsync(userId);

            return Ok(resultDto);
        }


        [Authorize]
        [HttpGet("users/me/{id}")]
        public async Task<IActionResult> GetMyFoundItemById(int id)
        {
            var userId = User.GetUserId();

            if (userId == null)
            {
                throw new UnauthorizedException("User is not authenticated");
            }
            var resultDto = await _service.GetMyFoundItemByIdAsync(id, userId);
    

            return Ok(resultDto);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetFoundItemById(int id)
        {
            var resultDto = await _service.GetFoundItemByIdAsync(id);
            if (resultDto == null) { throw new NotFoundException($"FoundItem does not exist with id {id}"); }
         
            return Ok(resultDto);
        }


   

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AddFoundItemDto dto, [FromForm] IFormFile? imageFile)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/found-items", imageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "imageFile is required." });
            }

            dto.ImageUrl = uploadedImageUrl;

            var userId = User.GetUserId();
            var userRole = User.IsInRole("lost_found_leader");

            

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated.");


            var createdItem = await _service.AddFoundItemAsync(dto, userId);


            var resultDto = _mapper.Map<GetFoundItemDto>(createdItem);

            return CreatedAtAction(
          nameof(GetFoundItemById),
          new { id = createdItem.Id },
          resultDto
      );

        }


        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateFoundItemDto dto, [FromForm] IFormFile? imageFile)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/found-items", imageFile);
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            // Ignore manual URL updates; image changes must come from uploaded file.
            dto.ImageUrl = string.IsNullOrWhiteSpace(uploadedImageUrl) ? null : uploadedImageUrl;

            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

         

            var resultDto = await _service.UpdateFoundItemAsync(id, dto, userId);
                
            return Ok(resultDto);
        }


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {


            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) { throw new UnauthorizedException("User is not authenticated."); }

            await _service.DeleteFoundItemAsync(id, userId);

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
