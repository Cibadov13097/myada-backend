using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Services
{
    public class LostItemAdminService : ILostItemAdminService
    {
        private readonly ILostItemRepository _repository;
        private readonly IMapper _mapper;

        public LostItemAdminService(ILostItemRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<AdminLostItemDto>> AdminGetAllAsync(bool includeDeleted)
        {
            var items = await _repository.GetAllForAdminAsync(includeDeleted);
            return _mapper.Map<List<AdminLostItemDto>>(items);
        }

        public async Task<AdminLostItemDto> AdminGetByIdAsync(int id)
        {
            var item = await _repository.GetByIdForAdminAsync(id)
                ?? throw new NotFoundException($"LostItem with id {id} not found");
            return _mapper.Map<AdminLostItemDto>(item);
        }

        public async Task<AdminLostItemDto> AdminAddLostItemAsync(AddLostItemDto dto, string adminId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

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
            if (dto == null) throw new ArgumentNullException(nameof(dto));
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
            var entity = await _repository.GetByIdForAdminAsync(itemId)
                ?? throw new NotFoundException($"LostItem with id {itemId} not found");

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }
    }
}
