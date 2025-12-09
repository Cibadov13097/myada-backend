using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
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
        public FoundItemService(IFoundItemRepository repository)
        {
            _repository = repository;
        }

        public async Task AddFoundItemAsync(FoundItem entity, string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.CreatedAt = DateTime.UtcNow;
            entity.FoundByUserId= userId;
            entity.CreatedByUserId = userId;
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
        }


            public async Task<List<FoundItem>> GetAllFoundItemsAsync()
            {
                var fountItems = await _repository.GetAllAsync();
                var filteredFoundItems= fountItems.Where(f=>f.IsDeleted==false);
                return fountItems;
            }

        public async Task<FoundItem> GetFoundItemByIdAsync(int id)
        {
            var foundItem = await _repository.GetByIdAsync(id);
            if (foundItem == null) throw new NotFoundException($"There is no FoundItem with id {id}");
            return foundItem;
        }

        public async Task<FoundItem> GetMyFoundItemByIdAsync(int id, string userId)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var foundItem = await _repository.GetByIdForUserAsync(id, userId);

            if (foundItem == null) throw new NotFoundException($"There is no FoundItem with id {id}");

            return foundItem;
        }

        public async Task<List<FoundItem>> GetMyFoundItemsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var foundItems = await _repository.GetAllByUserIdAsync(userId);
            return foundItems;
        }

        public async Task RemoveFoundItemAsync(int id, string userId)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid id");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var foundItem = await _repository.GetByIdForUserAsync(id,userId);

            if (foundItem == null)
                throw new NotFoundException($"Item with id {id} not found.");
            
           foundItem.DeletedByUserId = userId;
            foundItem.IsDeleted=true;
            await _repository.UpdateAsync(foundItem);
            await _repository.SaveChangesAsync();
        }

        public async Task UpdateFoundItemAsync(FoundItem entity, string userId)
        {
            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            if (entity.Id <= 0)
            {
                throw new ArgumentException("Invalid Id");
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }
            var foundItem = await _repository.GetByIdForUserAsync(entity.Id,userId);
            if (foundItem == null) throw new NotFoundException($"There is no FoundItem with id {entity.Id}");

            foundItem.ModifiedByUserId = userId;
            foundItem.ModifiedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(foundItem);
        }
    }
}
