using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface ISupportStaffStatusCrudService
    : ICrudService<SupportStaffStatus, SupportStaffListItemDto, SupportStaffStatusDto, CreateSupportStaffStatusDto, UpdateSupportStaffStatusDto>
{
}