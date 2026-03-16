using LostFoundService.Application.DTOs.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Interfaces.Services
{
    public interface ILocationServiceClient
    {
        Task<bool> LocationExistsAsync(int? buildingId, int? roomId);

        Task<LocationDto> GetLocationAsync(int? buildingId, int? roomId);
    }
}
