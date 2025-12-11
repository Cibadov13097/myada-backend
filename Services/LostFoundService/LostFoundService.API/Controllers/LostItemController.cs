using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LostFoundService.API.Controllers
{

    [Route("api/v1/lost-items")]
    [ApiController]
    public class LostItemController : ControllerBase
    {
        private readonly ILostItemService _lostItemService;
        private readonly IMapper _mapper;

        public LostItemController(
            ILostItemService lostItemService,
            IMapper mapper
            )
        {
            _lostItemService = lostItemService;
            _mapper = mapper;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lostItems = await _lostItemService.GetAllLostItemsAsync();

            var dto = _mapper.Map<List<GetLostItemDto>>(lostItems);
            return Ok(dto);
        }

 

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var lostItem = await _lostItemService.GetLostItemByIdAsync(id);
            if (lostItem == null)
            {
                return NotFound("Lost item not found");
            }
            var dto = _mapper.Map<GetLostItemDto>(lostItem);
            return Ok(dto);
        }

        [HttpPost()]
        public async Task<IActionResult> Create([FromForm] AddLostItemDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Dto cannot be null");
            }
            var entity = _mapper.Map<LostItem>(dto);
            await _lostItemService.AddLostItemAsync(entity);
            return Ok();
        }

        [HttpPut()]
        public async Task<IActionResult> Update([FromForm] UpdateLostItemDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Dto can not be empty");
            }
            var lostItem = await _lostItemService.GetLostItemByIdAsync(dto.Id);
            if (lostItem == null)
            {
                return NotFound("Lost item does not exist in db");
            }
            var entity = _mapper.Map(dto, lostItem);
            await _lostItemService.UpdateLostItemAsync(entity);
            return Ok();
        }

    }
}
