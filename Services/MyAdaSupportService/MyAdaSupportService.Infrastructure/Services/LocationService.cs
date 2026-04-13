using MyAdaSupportService.Application.Abstractions;

namespace MyAdaSupportService.Infrastructure.Services;

public sealed class LocationService : ILocationService
{
    public Task<string> ResolveLocationAsync(
        bool isBuilding,
        int? buildingId,
        bool isRoom,
        int? roomId,
        string? areaDetails)
    {
        var parts = new List<string>();
        if (isBuilding && buildingId is > 0)
            parts.Add($"Building {buildingId.Value}");
        if (isRoom && roomId is > 0)
            parts.Add($"Room {roomId.Value}");
        if (!string.IsNullOrWhiteSpace(areaDetails))
            parts.Add(areaDetails.Trim());

        var text = parts.Count > 0 ? string.Join(", ", parts) : "Location not specified";
        return Task.FromResult(text);
    }
}
