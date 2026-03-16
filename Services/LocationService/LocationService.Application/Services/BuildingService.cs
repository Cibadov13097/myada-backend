using LocationService.Application.DTOs;
using LocationService.Application.Interfaces.Repositories;
using LocationService.Application.Interfaces.Services;
using LocationService.Domain.Entities;

namespace LocationService.Application.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IGenericRepository<Building> _repository;

        public BuildingService(IGenericRepository<Building> repository)
        {
            _repository = repository;
        }

        public async Task<List<BuildingDto>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return items.Select(MapToDto).ToList();
        }

        public async Task<BuildingDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<BuildingDto> CreateAsync(CreateBuildingDto dto)
        {
            var entity = new Building
            {
                Name = dto.Name
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<BuildingDto> UpdateAsync(int id, UpdateBuildingDto dto)
        {
            var entity = await _repository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Building with id {id} not found");

            entity.Name = dto.Name;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"Building with id {id} not found");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }

        private static BuildingDto MapToDto(Building entity) =>
            new()
            {
                Id = entity.Id,
                Name = entity.Name
            };
    }
}

