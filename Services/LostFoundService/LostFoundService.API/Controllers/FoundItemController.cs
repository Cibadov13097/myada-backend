using AutoMapper;
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

namespace LostFoundService.Api.Controllers
{
    [Route("api/v1/found-items")]
    [ApiController]
    public class FoundItemController : ControllerBase
    {
        private readonly IFoundItemService _service;
        private readonly IMapper _mapper;
        public FoundItemController(
            IFoundItemService foundItemService,
            IMapper mapper
            )
        {
            _mapper = mapper;
            _service = foundItemService;
        }
        [HttpGet()]
        public async Task<IActionResult> GetFoundItems()
        {
            var foundItems = await _service.GetAllFoundItemsAsync();

            var dto = _mapper.Map<List<GetFoundItemDto>>(foundItems);

            return Ok(dto);
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

            var foundItems = await _service.GetMyFoundItemsAsync(userId);

            var dto= _mapper.Map<List<GetFoundItemDto>>(foundItems);

            return Ok(dto); 
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
            var foundItem=await _service.GetMyFoundItemByIdAsync(id,userId);
           var dto= _mapper.Map<GetFoundItemDto>(foundItem);

            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFoundItemById(int id)
        {
            var foundItem = await _service.GetFoundItemByIdAsync(id);
            if (foundItem == null) { throw new NotFoundException($"FoundItem does not exist with id {id}"); }
            var dto = _mapper.Map<GetFoundItemDto>(foundItem);
            return Ok(dto);
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

        [Authorize]
        [HttpPost()]
        public async Task<IActionResult> Create([FromForm] AddFoundItemDto dto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var foundItem = _mapper.Map<FoundItem>(dto);
            var userId = User.GetUserId();


            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated.");

            await _service.AddFoundItemAsync(foundItem, userId);
            return Ok();
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromForm] UpdateFoundItemDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var foundItem = _mapper.Map<FoundItem>(dto);
            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            await _service.UpdateFoundItemAsync(foundItem, userId);
            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
           

            var userId = User.GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) { throw new UnauthorizedException("User is not authenticated."); }

            await _service.RemoveFoundItemAsync(id, userId);

            return Ok();
        }
    }
}
