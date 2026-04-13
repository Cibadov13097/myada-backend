namespace MyAdaSupportService.Application.Abstractions;

public interface ILocationService
{
    Task<string> ResolveLocationAsync(
        bool isBuilding,
        int? buildingId,
        bool isRoom,
        int? roomId,
        string? areaDetails);
}
