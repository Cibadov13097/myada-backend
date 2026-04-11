using LocationService.Application.DTOs;
using LocationService.Application.Interfaces.Repositories;
using LocationService.Application.Interfaces.Services;
using LocationService.Domain.Entities;

namespace LocationService.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _repository;

        public RoomService(IRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoomDto>> GetAllAsync()
        {
            var items = await _repository.GetAllWithBuildingAsync();
            return items.Select(MapToDto).ToList();
        }

        public async Task<RoomDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdWithBuildingAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<List<RoomDto>> GetByBuildingIdAsync(int buildingId)
        {
            var items = await _repository.GetByBuildingIdWithBuildingAsync(buildingId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<RoomDto> CreateAsync(CreateRoomDto dto)
        {
            var entity = new Room
            {
                Name = dto.Name,
                Number = dto.Number,
                Capacity = dto.Capacity,
                RoomType = dto.RoomType,
                BuildingId = dto.BuildingId
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var created = await GetByIdAsync(entity.Id)
                         ?? throw new KeyNotFoundException($"Room with id {entity.Id} not found after creation");
            return created;
        }

        public async Task<RoomDto> UpdateAsync(int id, UpdateRoomDto dto)
        {
            var entity = await _repository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Room with id {id} not found");

            entity.Name = dto.Name;
            entity.Number = dto.Number;
            entity.Capacity = dto.Capacity;
            entity.RoomType = dto.RoomType;
            entity.BuildingId = dto.BuildingId;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            var updated = await GetByIdAsync(entity.Id)
                         ?? throw new KeyNotFoundException($"Room with id {entity.Id} not found after update");
            return updated;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Room with id {id} not found");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }

        private static RoomDto MapToDto(Room entity) =>
            new()
            {
                Id = entity.Id,
                Name = entity.Name,
                Number = entity.Number,
                Capacity = entity.Capacity,
                RoomType = entity.RoomType,
                BuildingId = entity.BuildingId,
                BuildingName = entity.Building?.Name
            };
    }
}

