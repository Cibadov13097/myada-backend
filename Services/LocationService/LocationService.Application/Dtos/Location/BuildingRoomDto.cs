using LocationService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocationService.Application.Dtos.Location
{
    public class BuildingRoomDto
    {
        public int BuildingId { get; set; }
        public string BuildingName { get; set; }

        public int? RoomId { get; set; }
        public string? RoomName { get; set; }
        public int? Capacity { get; set; }
        public RoomType? RoomType { get; set; }
    }
}
