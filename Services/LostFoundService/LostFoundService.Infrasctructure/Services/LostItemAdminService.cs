using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Infrastructure.Services
{
    public class LostItemAdminService : ILostItemAdminService
    {
        private readonly IMapper _mapper;
        private readonly ILostItemRepository _repository;

        public LostItemAdminService(
            IMapper mapper,
            ILostItemRepository repository)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public async Task<List<AdminLostItemDto>> AdminGetAllAsync(bool includeDeleted)
        {
            var entities = await _repository.GetAllForAdminAsync(includeDeleted);
            return _mapper.Map<List<AdminLostItemDto>>(entities);
        }

        public async Task<AdminLostItemDto> AdminGetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdForAdminAsync(id)
                ?? throw new NotFoundException($"LostItem with id {id} not found");

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task<AdminLostItemDto> AdminAddLostItemAsync(AddLostItemDto dto, string adminId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(adminId))
                throw new UnauthorizedException("Admin not authenticated");

            var entity = _mapper.Map<LostItem>(dto);

            entity.PostedByUserId = adminId;
            entity.CreatedByUserId = adminId;
            entity.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task<AdminLostItemDto> AdminUpdateAsync(int itemId, UpdateLostItemDto dto, string adminId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdForAdminAsync(itemId)
                ?? throw new NotFoundException($"LostItem with id {itemId} not found");

            _mapper.Map(dto, entity);

            entity.ModifiedByUserId = adminId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AdminLostItemDto>(entity);
        }

        public async Task AdminDeleteAsync(int itemId, string adminId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdForAdminAsync(itemId)
                ?? throw new NotFoundException($"LostItem with id {itemId} not found");

            entity.IsDeleted = true;
            entity.DeletedByUserId = adminId;
            entity.DeletedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task AdminForceDeleteAsync(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            var entity = await _repository.GetByIdForAdminAsync(itemId)
                ?? throw new NotFoundException($"LostItem with id {itemId} not found");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }
    }
}
