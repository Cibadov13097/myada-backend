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
        public async Task<IActionResult> GetMy() => Ok(await _service.GetMyLostItemsAsync(User.GetUserId()));


        [Authorize]
        [HttpGet("users/me/{id}")]
        public async Task<IActionResult> GetMyById(int id) => Ok(await _service.GetMyLostItemByIdAsync(id, User.GetUserId()));


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] AddLostItemDto dto)
        {
            var created = await _service.AddLostItemAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }


        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateLostItemDto dto)
        => Ok(await _service.UpdateLostItemAsync(id, dto, User.GetUserId()));


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteLostItemAsync(id, User.GetUserId());
            return NoContent();
        }
    }
}