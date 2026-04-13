using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;

public interface IAttachmentCrudService
    : ICrudService<Attachment, AttachmentDto, AttachmentDto, AttachmentCreateDto, AttachmentUpdateDto>
{
}
