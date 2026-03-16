using LostFoundService.Application.DTOs.Location;
using LostFoundService.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Services
{
    public class LocationServiceClient : ILocationServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocationServiceClient> _logger;
        public LocationServiceClient(HttpClient httpClient
            ,ILogger<LocationServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<LocationDto> GetLocationAsync(int? buildingId, int? roomId)
        {
            var response = await _httpClient.GetAsync($"api/v1/location/exist?");
            
            LocationDto locationDto = new LocationDto();
            return  locationDto;
                 
        }

        public async Task<bool> LocationExistsAsync(int? buildingId, int? roomId)
        {
            if (!buildingId.HasValue || !roomId.HasValue)
                return true;

            var body = new { RoomId = roomId.Value, BuildingId = buildingId.Value };
            var response = await _httpClient.PostAsJsonAsync("api/v1/location/validate-room-location", body);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var isValid = await response.Content.ReadFromJsonAsync<bool>();

            _logger.LogInformation($"response:  {response}");
            return isValid;
        }
    }
}
