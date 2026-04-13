using AutoMapper;
using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Application.Services;

public class AttachmentService
    : CrudManager<Attachment, AttachmentDto, AttachmentDto, AttachmentCreateDto, AttachmentUpdateDto>,
      IAttachmentCrudService,
      IAttachmentService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IMapper _mapper;
    private readonly ISupportRealtimeNotifier _realtimeNotifier;

    public AttachmentService(
        IAttachmentRepository attachmentRepository,
        IMapper mapper,
        ISupportRealtimeNotifier realtimeNotifier)
        : base(attachmentRepository, mapper)
    {
        _attachmentRepository = attachmentRepository;
        _mapper = mapper;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<AttachmentDto> AddAttachmentAsync(AttachmentCreateDto dto)
    {
        var created = await AddAsync(dto);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(dto.RequestId, "attachment-added");
        return created;
    }

    public async Task<List<AttachmentDto>> GetRequestAttachmentsAsync(int requestId)
    {
        var attachments = await _attachmentRepository.GetByRequestIdAsync(requestId);
        return _mapper.Map<List<AttachmentDto>>(attachments);
    }

    public async Task RemoveAttachmentAsync(int attachmentId)
    {
        var existing = await _attachmentRepository.GetByIdAsync(attachmentId);
        await RemoveAsync(attachmentId);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(existing.RequestId, "attachment-removed");
    }
}