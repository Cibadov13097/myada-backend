
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
    public class FoundItemController : ControllerBase
    {
        private readonly IFoundItemService _service;
  
        public FoundItemController(
            IFoundItemService foundItemService
        
            )
        {

       
            _service = foundItemService;
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

        [Authorize]
        [HttpGet("debug")]
        public IActionResult DebugAuth()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }



        [Authorize(Roles = "lost_found_leader")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AddFoundItemDto dto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
          var  userId = User.FindFirst(ClaimsIdentity.DefaultNameClaimType).Value;
          var resultDto= await _service.AddFoundItemAsync(dto, userId);
            return Ok(resultDto);
        }



        [Authorize(Roles = "lost_found_leader,admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id,[FromForm] UpdateFoundItemDto dto)
        {
          
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
        public async Task<IActionResult> Delete(int id)
        {


            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) { throw new UnauthorizedException("User is not authenticated."); }

            await _service.DeleteFoundItemAsync(id, userId);

            return NoContent();
        }


        [HttpDelete("{id}/force")]
        public async Task<IActionResult> ForceDelete(int id)
        {


            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) { throw new UnauthorizedException("User is not authenticated."); }

            await _service.AdminForceDeleteAsync(id, userId);

            return NoContent();
        }



    }
}
