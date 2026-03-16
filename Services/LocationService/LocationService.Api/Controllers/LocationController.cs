using LocationService.Application.DTOs;
using LocationService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocationService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/locations")]
    [Tags("Location")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }


        [HttpPost("validate-room-location")]
        public async Task<IActionResult> ValidateRoomLocationAsync([FromBody] ValidateRoomLocationDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var isValid = await _locationService.ValidateRoomLocationAsync(request);
            //if (!isValid)
            //{
            //    return NotFound("RoomId və LocationId üçün uyğun əlaqə tapılmadı.");
            //}

            return Ok(isValid);
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetLocationAsync()
        {
           
            return Ok();
        }
    }
}

