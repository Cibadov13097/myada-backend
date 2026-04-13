using AutoMapper;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Domain.Entities.BaseAggregate;
using MyAdaSupportService.Application.Abstractions;
using System.Linq.Expressions;

namespace MyAdaSupportService.Application.Services;

public class CrudManagerWithoutUpdate<TEntity, TListDto, TDetailDto, TCreateDto>
    : ICrudServiceWithoutUpdate<TEntity, TListDto, TDetailDto, TCreateDto>
    where TEntity : BaseEntity
{
    protected readonly IRepository<TEntity> Repository;
    protected readonly IMapper Mapper;

    public CrudManagerWithoutUpdate(IRepository<TEntity> repository, IMapper mapper)
    {
        Repository = repository;
        Mapper = mapper;
    }

    public virtual async Task<TDetailDto> AddAsync(TCreateDto createDto)
    {
        var entity = Mapper.Map<TEntity>(createDto);
        var result = await Repository.AddAsync(entity);
        return Mapper.Map<TDetailDto>(result);
    }

    public virtual async Task<TDetailDto> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool asNoTracking = false,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null)
    {
        var entity = await Repository.GetAsync(predicate, include, asNoTracking);
        return Mapper.Map<TDetailDto>(entity);
    }

    public virtual async Task<List<TListDto>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool asNoTracking = false,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        var entities = await Repository.GetAllAsync(predicate, include, orderBy, asNoTracking);
        return Mapper.Map<List<TListDto>>(entities);
    }

    public virtual async Task<TDetailDto> GetByIdAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return Mapper.Map<TDetailDto>(entity);
    }

    public virtual async Task RemoveAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);

        if (entity == null)
            throw new Exception("Entity not found");

        await Repository.RemoveAsync(entity);
    }
}