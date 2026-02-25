using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Services
{
    public class FoundItemAdminService:IFoundItemAdminService
    {
        private readonly IMapper _mapper;
        private readonly IFoundItemRepository _repository;
        public FoundItemAdminService(IMapper mapper,
            IFoundItemRepository foundItemRepository)
        {
            _mapper = mapper;
            _repository = foundItemRepository;
        }

        public async Task<GetFoundItemDto> AdminAddFoundItemAsync(AddFoundItemDto dto,string adminId)
        {
        
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entity = _mapper.Map<FoundItem>(dto);



            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId = adminId;
            entity.CreatedByUserId = adminId;
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            entity.Status=Status.approved;
            var resultDto = _mapper.Map<GetFoundItemDto>(entity);
            return resultDto;
        }

        public async Task AdminDeleteAsync(int itemId, string adminId)
        {
   
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(adminId))
                throw new ArgumentNullException(nameof(adminId));

            var entity = await _repository.GetByIdAsync(itemId);

            if (entity == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            entity.DeletedByUserId = adminId;
            entity.DeletedAt = DateTime.UtcNow;
            entity.IsDeleted = true;
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task AdminForceDeleteAsync(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");


            var foundItem = await _repository.GetByIdForAdminAsync(itemId);
            if (foundItem == null)
                throw new NotFoundException($"Item with id {itemId} not found.");


            await _repository.DeleteAsync(foundItem);
            await _repository.SaveChangesAsync();


        }

        public async Task<List<AdminFoundItemDto>> AdminGetAllAsync(bool includeDeleted)
        {
            var entities = await _repository.GetAllForAdminAsync(includeDeleted);
            var dto = _mapper.Map<List<AdminFoundItemDto>>(entities);

            return dto;
        }

        public async Task<AdminFoundItemDto> AdminGetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdForAdminAsync(id);

            var dto = _mapper.Map<AdminFoundItemDto>(entity);

            return dto;
        }

        public async Task<GetFoundItemDto> AdminUpdateAsync(int itemId, UpdateFoundItemDto dto, string adminId)
        {
            if (dto == null)
            {
                throw new ArgumentNullException("Dto is empty");
            }
            if (itemId <= 0)
            {
                throw new ArgumentException("Invalid Id");
            }

            var entity = await _repository.GetByIdAsync(itemId);
            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {itemId}");

            _mapper.Map(dto, entity);

            entity.ModifiedByUserId = adminId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();


            var resultDto = _mapper.Map<GetFoundItemDto>(entity);
            return resultDto;
        }

        public async Task<GetFoundItemDto> AdminUpdateStatusAsync(int itemId, Status status, string adminId)
        {
            if (status == null)
            {
                throw new ArgumentNullException("Dto is empty");
            }
            if (itemId <= 0)
            {
                throw new ArgumentException("Invalid Id");
            }

            var entity = await _repository.GetByIdAsync(itemId);

            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {itemId}");


            entity.Status = status;
            entity.ModifiedByUserId = adminId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();


            var resultDto = _mapper.Map<GetFoundItemDto>(entity);
            return resultDto;
        }

    
    }
}
