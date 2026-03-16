using LocationService.Application.DTOs;

namespace LocationService.Application.Interfaces.Services
{
    public interface ILocationService
    {
        /// <summary>
        /// roomId və locationId (praktikada BuildingId) bir-biri ilə əlaqəlidirmi
        /// və hər ikisi mövcuddurmu onu yoxlayır.
        /// </summary>
        Task<bool> ValidateRoomLocationAsync(ValidateRoomLocationDto request);
    }
}

