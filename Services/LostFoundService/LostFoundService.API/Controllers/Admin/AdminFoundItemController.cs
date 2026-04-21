
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.Extensions;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;
using LostFoundService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LostFoundService.Api.Controllers.Admin
{
    [Route("api/v1/admin/found-items")]
    [ApiController]
    [Tags("Found Items - Admin")]

    public class AdminFoundItemController : ControllerBase
    {
        private readonly IFoundItemAdminService _service;
        private readonly IImageStorageService _imageStorageService;
  
        public AdminFoundItemController(
            IFoundItemAdminService foundItemService,
            IImageStorageService imageStorageService
        
            )
        {

       
            _service = foundItemService;
            _imageStorageService = imageStorageService;
        }


        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpGet()]
        public async Task<IActionResult> GetFoundItems( [FromQuery] bool includeDeleted = false)
        {
            var resultDto = await _service.AdminGetAllAsync(includeDeleted);


            return Ok(resultDto);
        }


        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFoundItemById(int id)
        {
            var resultDto = await _service.AdminGetByIdAsync(id);
            if (resultDto == null) { throw new NotFoundException($"FoundItem does not exist with id {id}"); }
            
            return Ok(resultDto);
        }

     

        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AddFoundItemDto dto)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/found-items/admin");
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                dto.ImageUrl = uploadedImageUrl;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var adminId = User.GetUserId();

            if(adminId == null)
            {
                throw new UnauthorizedException("Admin not found!");
            }
          var resultDto= await _service.AdminAddFoundItemAsync(dto,adminId);
            return Ok(resultDto);
        }



        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id,[FromForm] UpdateFoundItemDto dto)
        {
            var uploadedImageUrl = await UploadFormImageIfPresentAsync("legacy/found-items/admin");
            if (uploadedImageUrl == string.Empty)
            {
                return BadRequest(new { error = "VALIDATION_ERROR", message = "Only PNG/JPG files are supported." });
            }

            if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
            {
                dto.ImageUrl = uploadedImageUrl;
            }

          
            var adminId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            var resultDto=await _service.AdminUpdateAsync(id, dto,adminId);
            return Ok(resultDto);
        }

        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpDelete("{id}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(int id)
        {


            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) { throw new UnauthorizedException("User is not authenticated."); }

            await _service.AdminDeleteAsync(id, userId);

            return NoContent();
        }


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
