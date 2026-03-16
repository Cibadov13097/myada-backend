using System.ComponentModel.DataAnnotations;

namespace LocationService.Application.DTOs
{
    public class ValidateRoomLocationDto
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public int BuildingId { get; set; }
    }
}

