using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface IRequestCrudService
    : ICrudService<Request, SupportRequestListItemDto, SupportRequestDetailDto, CreateSupportRequestDto, UpdateSupportRequestDto>
{
}
