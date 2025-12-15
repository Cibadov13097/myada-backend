using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Infrastructure.Services
{
    public class LostItemService : ILostItemService
    {
        private readonly ILostItemRepository _repository;
        private readonly IMapper _mapper;

        public LostItemService(ILostItemRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<AdminLostItemDto>> GetAllLostItemsAsync()
        {
            var items = await _repository.GetAllAsync();
            return _mapper.Map<List<AdminLostItemDto>>(items);
        }

        public async Task<AdminLostItemDto> GetLostItemByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

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
            if (id <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var item = await _repository.GetByIdForUserAsync(id, userId)
                ?? throw new NotFoundException($"LostItem with id {id} not found");

            return _mapper.Map<AdminLostItemDto>(item);
        }

        public async Task<AdminLostItemDto> AddLostItemAsync(AddLostItemDto dto, string userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var entity = _mapper.Map<LostItem>(dto);

            entity.PostedByUserId = userId;
            entity.CreatedByUserId = userId;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task<AdminLostItemDto> UpdateLostItemAsync(int id, UpdateLostItemDto dto, string userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (id <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated");

            var entity = await _repository.GetByIdForUserAsync(id, userId)
                ?? throw new NotFoundException($"LostItem with id {id} not found");

            _mapper.Map(dto, entity);

            entity.ModifiedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task DeleteLostItemAsync(int id, string userId)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

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
