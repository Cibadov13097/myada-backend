using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Services
{
    public class LostItemService : ILostItemService
    {
        private readonly IGenericRepository<LostItem> _genericRepository;

        public LostItemService(IGenericRepository<LostItem> genericRepository)
        {
            _genericRepository = genericRepository;
        }



        public async Task AddLostItemAsync(LostItem entity)
        {
            if(entity==null) throw new ArgumentNullException(nameof(entity));
            entity.CreatedAt= DateTime.UtcNow;
            await _genericRepository.AddAsync(entity);
            await _genericRepository.SaveChangesAsync();
        }

        public async Task<List<LostItem>> GetAllLostItemsAsync()
        {
            return await _genericRepository.GetAllAsync();
        }

        public async Task<LostItem> GetLostItemByIdAsync(int id)
        {
            return await _genericRepository.GetByIdAsync(id);
        }

        public Task<LostItem> GetMyLostItemAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateLostItemAsync(LostItem entity)
        {
            entity.ModifiedAt= DateTime.UtcNow;
            await _genericRepository.UpdateAsync(entity);
            await _genericRepository.SaveChangesAsync();
        }
    }
}
