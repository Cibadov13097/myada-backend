using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.DTOs.Request;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface IAttachmentService
{
    Task<AttachmentDto> AddAttachmentAsync(AttachmentCreateDto dto);
    Task<List<AttachmentDto>> GetRequestAttachmentsAsync(int requestId);
    Task RemoveAttachmentAsync(int attachmentId);
}
