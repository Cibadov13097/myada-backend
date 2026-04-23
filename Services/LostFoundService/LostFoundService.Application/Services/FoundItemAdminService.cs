using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Services
{
    public class FoundItemAdminService : IFoundItemAdminService
    {
        private readonly IFoundItemRepository _repository;
        private readonly IMapper _mapper;

        public FoundItemAdminService(IFoundItemRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<GetFoundItemDto> AdminAddFoundItemAsync(AddFoundItemDto dto, string adminId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (!string.IsNullOrWhiteSpace(dto.Location) && dto.Location.Trim().Length > 400)
                throw new ArgumentException("Location max length is 400.");

            var entity = _mapper.Map<FoundItem>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId = adminId;
            entity.CreatedByUserId = adminId;
            entity.Status = Status.approved;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }

        public async Task AdminDeleteAsync(int itemId, string adminId)
        {
            if (itemId <= 0) throw new ArgumentException("Invalid id");
            if (string.IsNullOrWhiteSpace(adminId)) throw new ArgumentNullException(nameof(adminId));

            var entity = await _repository.GetByIdForAdminAsync(itemId);
            if (entity == null) throw new NotFoundException($"Item with id {itemId} not found.");

            entity.DeletedByUserId = adminId;
            entity.DeletedAt = DateTime.UtcNow;
            entity.IsDeleted = true;
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task AdminForceDeleteAsync(int itemId)
        {
            if (itemId <= 0) throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdForAdminAsync(itemId);
            if (entity == null) throw new NotFoundException($"Item with id {itemId} not found.");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task<List<AdminFoundItemDto>> AdminGetAllAsync(bool includeDeleted)
        {
            var items = await _repository.GetAllForAdminAsync(includeDeleted);
            return _mapper.Map<List<AdminFoundItemDto>>(items);
        }

        public async Task<AdminFoundItemDto> AdminGetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdForAdminAsync(id);
            if (entity == null) throw new NotFoundException($"FoundItem with id {id} not found.");
            return _mapper.Map<AdminFoundItemDto>(entity);
        }

        public async Task<GetFoundItemDto> AdminUpdateAsync(int itemId, UpdateFoundItemDto dto, string adminId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (itemId <= 0) throw new ArgumentException("Invalid Id");
            if (!string.IsNullOrWhiteSpace(dto.Location) && dto.Location.Trim().Length > 400)
                throw new ArgumentException("Location max length is 400.");

            var entity = await _repository.GetByIdForAdminAsync(itemId);
            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {itemId}");

            _mapper.Map(dto, entity);
            entity.ModifiedByUserId = adminId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }

        public async Task<GetFoundItemDto> AdminUpdateStatusAsync(int itemId, Status status, string adminId)
        {
            if (itemId <= 0) throw new ArgumentException("Invalid Id");

            var entity = await _repository.GetByIdForAdminAsync(itemId);
            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {itemId}");

            entity.Status = status;
            entity.ModifiedByUserId = adminId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<GetFoundItemDto>(entity);
        }
    }
}
