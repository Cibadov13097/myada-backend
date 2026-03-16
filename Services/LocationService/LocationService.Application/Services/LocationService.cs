using LocationService.Application.Dtos.Location;
using LocationService.Application.DTOs;
using LocationService.Application.Interfaces.Repositories;
using LocationService.Application.Interfaces.Services;
using LocationService.Domain.Entities;

namespace LocationService.Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly IGenericRepository<Room> _roomRepository;
        private readonly IGenericRepository<Building> _buildingRepository;

        public LocationService(
            IGenericRepository<Room> roomRepository,
            IGenericRepository<Building> buildingRepository)
        {
            _roomRepository = roomRepository;
            _buildingRepository = buildingRepository;
        }

        /// <summary>
        /// Validates that the room exists and belongs to the given building.
        /// </summary>
        public async Task<bool> ValidateRoomLocationAsync(ValidateRoomLocationDto request)
        {
            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room is null)
                return false;

            var building = await _buildingRepository.GetByIdAsync(request.BuildingId);
            if (building is null)
                return false;

            return room.BuildingId == request.BuildingId;
        }
        //public async Task<List<BuildingRoomDto>> GetBuildingsRooms()
        //{

        //    return
        //}
    }
}

