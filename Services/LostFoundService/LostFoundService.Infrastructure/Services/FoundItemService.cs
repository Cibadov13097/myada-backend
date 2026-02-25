using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using LostFoundService.Domain.Exceptions;
using LostFoundService.Infrastructure.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Services
{
    public class FoundItemService : IFoundItemService
    {
        private readonly IFoundItemRepository _repository;
        private readonly IMapper _mapper;
        public FoundItemService(IFoundItemRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<GetFoundItemDto> AddFoundItemAsync(AddFoundItemDto dto, string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entity = _mapper.Map<FoundItem>(dto);



            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId = userId;
            entity.CreatedByUserId = userId;
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var resultDto = _mapper.Map<GetFoundItemDto>(entity);
            return resultDto;
        }


        public async Task<List<GetFoundItemDto>> GetAllFoundItemsAsync()
        {
            var foundItems = await _repository.GetAllAsync();
            var dto = _mapper.Map<List<GetFoundItemDto>>(foundItems);
            return dto;
        }

        public async Task<GetFoundItemDto> GetFoundItemByIdAsync(int id)
        {
            var foundItem = await _repository.GetByIdAsync(id);
            if (foundItem == null) throw new NotFoundException($"There is no FoundItem with id {id}");

            var dto = _mapper.Map<GetFoundItemDto>(foundItem);
            return dto;
        }

        public async Task<GetFoundItemDto> GetMyFoundItemByIdAsync(int id, string userId)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var foundItem = await _repository.GetByIdForUserAsync(id, userId);

            if (foundItem == null) throw new NotFoundException($"There is no FoundItem with id {id}");

            var dto = _mapper.Map<GetFoundItemDto>(foundItem);

            return dto;
        }

        public async Task<List<GetFoundItemDto>> GetMyFoundItemsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var entity = await _repository.GetAllByUserIdAsync(userId);

            var dto = _mapper.Map<List<GetFoundItemDto>>(entity);

            return dto;
        }


        public async Task DeleteFoundItemAsync(int itemId, string userId)
        {
            if (itemId <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var foundItem = await _repository.GetByIdForUserAsync(itemId, userId);

            if (foundItem == null)
                throw new NotFoundException($"Item with id {itemId} not found.");

            foundItem.DeletedByUserId = userId;
            foundItem.DeletedAt = DateTime.UtcNow;
            foundItem.IsDeleted = true;
            await _repository.UpdateAsync(foundItem);
            await _repository.SaveChangesAsync();
        }

        //public async Task RemoveFoundItemAsync(int id, string userId)
        //{
        //    if (id <= 0)
        //        throw new ArgumentException("Invalid id");

        //    if (string.IsNullOrWhiteSpace(userId))
        //        throw new ArgumentNullException(nameof(userId));

        //    var foundItem = await _repository.GetByIdForUserAsync(id, userId);

        //    if (foundItem == null)
        //        throw new NotFoundException($"Item with id {id} not found.");

        //    foundItem.DeletedByUserId = userId;
        //    foundItem.DeletedAt = DateTime.UtcNow;
        //    foundItem.IsDeleted = true;
        //    await _repository.UpdateAsync(foundItem);
        //    await _repository.SaveChangesAsync();
        //}

        public async Task<GetFoundItemDto> UpdateFoundItemAsync(int id, UpdateFoundItemDto dto, string userId)
        {
            if (dto == null)
            {
                throw new ArgumentNullException("Dto is empty");
            }
            if (id <= 0)
            {
                throw new ArgumentException("Invalid Id");
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            var entity = await _repository.GetByIdForUserAsync(id, userId);
            if (entity == null) throw new NotFoundException($"There is no FoundItem with id {id}");

            _mapper.Map(dto, entity);

            entity.ModifiedByUserId = userId;
            entity.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            var resultDto = _mapper.Map<GetFoundItemDto>(entity);

            return resultDto;
        }
    }
}
