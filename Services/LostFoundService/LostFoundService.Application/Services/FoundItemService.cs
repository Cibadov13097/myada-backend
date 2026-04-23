using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Services
{
    public class FoundItemService : IFoundItemService
    {
        private readonly IFoundItemRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILocationServiceClient _locationServiceClient;

        public FoundItemService(IFoundItemRepository repository, IMapper mapper,
            ILocationServiceClient locationServiceClient)
        {
            _repository = repository;
            _mapper = mapper;
            _locationServiceClient = locationServiceClient;
        }

        public async Task<GetFoundItemDto> AddFoundItemAsync(AddFoundItemDto dto, string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!string.IsNullOrWhiteSpace(dto.Location) && dto.Location.Trim().Length > 400)
                throw new ArgumentException("Location max length is 400.");

            if (dto.BuildingId.HasValue != dto.RoomId.HasValue)
                throw new ArgumentException("Location requires both BuildingId and RoomId when provided.");

            if (dto.BuildingId.HasValue && dto.RoomId.HasValue)
            {
                bool exists;
                try
                {
                    exists = await _locationServiceClient.LocationExistsAsync(dto.BuildingId, dto.RoomId);
                }
                catch (Exception)
                {
                    throw new ServiceUnavailableException("Location service is unavailable");
                }

                if (!exists)
                    throw new NotFoundException("Building or Room not found.");
            }

            var entity = _mapper.Map<FoundItem>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId = userId;
            entity.CreatedByUserId = userId;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }

        public async Task<List<GetFoundItemDto>> GetAllFoundItemsAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<List<GetFoundItemDto>>(items);
        }

        public async Task<GetFoundItemDto> GetFoundItemByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) throw new NotFoundException($"There is no FoundItem with id {id}");
            return _mapper.Map<GetFoundItemDto>(item);
        }

        public async Task<GetFoundItemDto> GetMyFoundItemByIdAsync(int id, string userId)
        {
            if (id <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            var item = await _repository.GetByIdForUserAsync(id, userId);
            if (item == null) throw new NotFoundException($"There is no FoundItem with id {id}");
            return _mapper.Map<GetFoundItemDto>(item);
        }

        public async Task<List<GetFoundItemDto>> GetMyFoundItemsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            var items = await _repository.GetAllByUserIdAsync(userId);
            return _mapper.Map<List<GetFoundItemDto>>(items);
        }

        public async Task DeleteFoundItemAsync(int itemId, string userId)
        {
            if (itemId <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            var item = await _repository.GetByIdForUserAsync(itemId, userId);
            if (item == null) throw new NotFoundException($"Item with id {itemId} not found.");

            item.DeletedByUserId = userId;
            item.DeletedAt = DateTime.UtcNow;
            item.IsDeleted = true;
            await _repository.UpdateAsync(item);
            await _repository.SaveChangesAsync();
        }

        public async Task<GetFoundItemDto> UpdateFoundItemAsync(int id, UpdateFoundItemDto dto, string userId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (id <= 0) throw new ArgumentException("Invalid Id");
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (!string.IsNullOrWhiteSpace(dto.Location) && dto.Location.Trim().Length > 400)
                throw new ArgumentException("Location max length is 400.");

            var entity = await _repository.GetByIdForUserAsync(id, userId);
            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {id}");

            if (dto.BuildingId.HasValue != dto.RoomId.HasValue)
                throw new ArgumentException("Location requires both BuildingId and RoomId when provided.");

            if (dto.BuildingId.HasValue && dto.RoomId.HasValue)
            {
                bool exists;
                try
                {
                    exists = await _locationServiceClient.LocationExistsAsync(dto.BuildingId, dto.RoomId);
                }
                catch (Exception)
                {
                    throw new ServiceUnavailableException("Location service is unavailable");
                }

                if (!exists)
                    throw new NotFoundException("Building or Room not found.");

            }

            _mapper.Map(dto, entity);
            entity.ModifiedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }
    }
}
