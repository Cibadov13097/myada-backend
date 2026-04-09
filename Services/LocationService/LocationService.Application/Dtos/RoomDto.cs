using LocationService.Domain.Enums;

namespace LocationService.Application.DTOs
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Number { get; set; }
        public int Capacity { get; set; }
        public RoomType RoomType { get; set; }
        public int BuildingId { get; set; }
    }

    public class CreateRoomDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Number { get; set; }
        public int Capacity { get; set; }
        public RoomType RoomType { get; set; }
        public int BuildingId { get; set; }
    }

    public class UpdateRoomDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Number { get; set; }
        public int Capacity { get; set; }
        public RoomType RoomType { get; set; }
        public int BuildingId { get; set; }
    }

    public class ValidateRoomLocationDto
    {
        public int RoomId { get; set; }
        public int BuildingId { get; set; }
    }
}

