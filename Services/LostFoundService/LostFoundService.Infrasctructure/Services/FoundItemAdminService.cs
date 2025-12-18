using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Infrastructure.Services
{
    public class FoundItemAdminService : IFoundItemAdminService
    {
        private readonly IMapper _mapper;
        private readonly IFoundItemRepository _repository;

        public FoundItemAdminService(
            IMapper mapper,
            IFoundItemRepository foundItemRepository)
        {
            _mapper = mapper;
            _repository = foundItemRepository;
        }

        public async Task<GetFoundItemDto> AddAsync(AddFoundItemDto dto, string adminId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entity = _mapper.Map<FoundItem>(dto);

            entity.Status = Status.approved;
            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId = adminId;
            entity.CreatedByUserId = adminId;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }

        public async Task DeleteAsync(int itemId, string adminId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(adminId))
                throw new ArgumentNullException(nameof(adminId));

            var entity = await _repository.GetByIdAsync(itemId);
            if (entity == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = adminId;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task ForceDeleteAsync(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdAsync(itemId);
            if (entity == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task<List<AdminGetFoundItemDto>> GetAllAsync(bool includeDeleted)
        {
            var query = await _repository.GetAllAsync(includeDeleted);

            var items =  query.ToList();
            return _mapper.Map<List<AdminGetFoundItemDto>>(items);
        }

        public async Task<List<GetFoundItemDto>> GetByCategoryIdAsync(int categoryId)
        {
            var entities = await _repository.GetByCategoryIdAsync(categoryId);

            return _mapper.Map<List<GetFoundItemDto>>(entities);
        }

        public async Task<AdminGetFoundItemDto> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException($"Item with id {id} not found.");

            return _mapper.Map<AdminGetFoundItemDto>(entity);
        }

        public async Task<GetFoundItemDto> UpdateAsync(int itemId, UpdateFoundItemDto dto, string adminId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdAsync(itemId);
            if (entity == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            _mapper.Map(dto, entity);

            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = adminId;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }

        public async Task<GetFoundItemDto> UpdateStatusAsync(int itemId, Status status, string adminId)
        {
            if (!Enum.IsDefined(typeof(Status), status))
                throw new ArgumentException("Invalid status");

            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdAsync(itemId);
            if (entity == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            entity.Status = status;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedByUserId = adminId;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }
    
    
    }
}
