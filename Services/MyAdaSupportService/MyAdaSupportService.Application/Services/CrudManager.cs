using AutoMapper;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Domain.Entities.BaseAggregate;

namespace MyAdaSupportService.Application.Services;

public class CrudManager<TEntity, TListDto, TDetailDto, TCreateDto, TUpdateDto>
    : CrudManagerWithoutUpdate<TEntity, TListDto, TDetailDto, TCreateDto>,
      ICrudService<TEntity, TListDto, TDetailDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity
{
    public CrudManager(IRepository<TEntity> repository, IMapper mapper)
        : base(repository, mapper)
    {
    }

    public virtual async Task<TDetailDto?> UpdateAsync(int id, TUpdateDto updateDto)
    {
        var entity = await Repository.GetByIdAsync(id);

        if (entity == null)
            return default;

        Mapper.Map(updateDto, entity);
        var result = await Repository.UpdateAsync(entity);

        return Mapper.Map<TDetailDto>(result);
    }
}

