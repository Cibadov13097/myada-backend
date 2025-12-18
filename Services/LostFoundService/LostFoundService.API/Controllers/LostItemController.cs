using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.Extensions;
using LostFoundService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostFoundService.Api.Controllers
{
    [Route("api/v1/lost-items")]
    [ApiController]
    [Tags("Lost Items")]

    public class LostItemController : ControllerBase
    {
        private readonly ILostItemService _service;
        public LostItemController(ILostItemService service) => _service = service;


        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllLostItemsAsync());


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _service.GetLostItemByIdAsync(id));


        [Authorize]
        [HttpGet("users/me")]
        public async Task<IActionResult> GetMy()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User is not authenticated");
            return Ok(await _service.GetMyLostItemsAsync(userId));
        }


        [Authorize]
        [HttpGet("users/me/{id}")]
        public async Task<IActionResult> GetMyById(int id)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User is not authenticated");
            return Ok(await _service.GetMyLostItemByIdAsync(id, userId));
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AddLostItemDto dto)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User is not authenticated");
            var created = await _service.AddLostItemAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }


        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLostItemDto dto)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User is not authenticated");
            return Ok(await _service.UpdateLostItemAsync(id, dto, userId));
        }


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User is not authenticated");
            await _service.DeleteLostItemAsync(id, userId);
            return NoContent();
        }
    }
}