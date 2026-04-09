using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Application.Interfaces;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events.LostFound;
using Shared.Shared.Infrastructure;
using System.Net;

namespace LostFoundService.Application.Services
{
    public class LostItemService : ILostItemService
    {
        private readonly ILostItemRepository _repository;
        private readonly IMapper _mapper;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILocationServiceClient _locationServiceClient;
        private readonly ILogger<LostItemService> _logger;

        public LostItemService(ILostItemRepository repository,
            IMapper mapper,
            IRabbitMqPublisher rabbitMqPublisher,
            ILocationServiceClient locationServiceClient,
            ILogger<LostItemService> logger )
        {
            _repository = repository;
            _mapper = mapper;
            _rabbitMqPublisher = rabbitMqPublisher;
            _locationServiceClient = locationServiceClient;
            _logger = logger;
        }

        public async Task<List<AdminLostItemDto>> GetAllLostItemsAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<List<AdminLostItemDto>>(items);
        }

        public async Task<AdminLostItemDto> GetLostItemByIdAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("Invalid id");
            var item = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException($"LostItem with id {id} not found");
            return _mapper.Map<AdminLostItemDto>(item);
        }

        public async Task<List<AdminLostItemDto>> GetMyLostItemsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");
            var items = await _repository.GetAllByUserIdAsync(userId);
            return _mapper.Map<List<AdminLostItemDto>>(items);
        }

        public async Task<AdminLostItemDto> GetMyLostItemByIdAsync(int id, string userId)
        {
            if (id <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");
            var item = await _repository.GetByIdForUserAsync(id, userId)
                ?? throw new NotFoundException($"LostItem with id {id} not found");
            return _mapper.Map<AdminLostItemDto>(item);
        }

        public async Task<AdminLostItemDto> AddLostItemAsync(AddLostItemDto dto, string userId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

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

                _logger.LogInformation($"status:{exists}");
            }

           
            var entity = _mapper.Map<LostItem>(dto);
            entity.PostedByUserId = userId;
            entity.CreatedByUserId = userId;
            entity.CreatedAt = DateTime.UtcNow;

            
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

           

            var locationDisplay = entity.BuildingId.HasValue || entity.RoomId.HasValue
                ? $"Building {entity.BuildingId}, Room {entity.RoomId}"
                : string.Empty;
            var evt = new LostItemCreatedEvent
            {
                LostItemId = entity.Id,
                Name = entity.Title,
                Location = locationDisplay
            };
            //await _rabbitMqPublisher.PublishAsync("notification-queue", evt);
            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task<AdminLostItemDto> UpdateLostItemAsync(int id, UpdateLostItemDto dto, string userId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (id <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var entity = await _repository.GetByIdForUserAsync(id, userId)
                ?? throw new NotFoundException($"LostItem with id {id} not found");

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

                _logger.LogWarning($"status:{exists}");
            }

            _mapper.Map(dto, entity);
            entity.ModifiedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task DeleteLostItemAsync(int id, string userId)
        {
            if (id <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var entity = await _repository.GetByIdForUserAsync(id, userId)
                ?? throw new NotFoundException($"LostItem with id {id} not found");

            entity.IsDeleted = true;
            entity.DeletedByUserId = userId;
            entity.DeletedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }
    }
}
