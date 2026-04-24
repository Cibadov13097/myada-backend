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
    private readonly IRequestRepository _requestRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ISupportRealtimeNotifier _realtimeNotifier;

    public AttachmentService(
        IAttachmentRepository attachmentRepository,
        IRequestRepository requestRepository,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ISupportRealtimeNotifier realtimeNotifier)
        : base(attachmentRepository, mapper)
    {
        _attachmentRepository = attachmentRepository;
        _requestRepository = requestRepository;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<AttachmentDto> AddAttachmentAsync(AttachmentCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
        {
            throw new ArgumentException("Attachment URL is required.", nameof(dto));
        }

        if (!_fileStorageService.IsManagedImageUrl(dto.Url))
        {
            throw new InvalidOperationException("Attachment URLs must be uploaded image URLs from SupportRequests uploads.");
        }

        var request = await _requestRepository.GetByIdAsync(dto.RequestId);
        var created = await AddAsync(dto);

        if (string.IsNullOrWhiteSpace(request.ImgUrl))
        {
            request.ImgUrl = created.Url;
            await _requestRepository.UpdateAsync(request);
        }

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

        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(existing.RequestId);
        if (request != null && string.Equals(request.ImgUrl, existing.Url, StringComparison.OrdinalIgnoreCase))
        {
            request.ImgUrl = request.Attachments
                .OrderBy(x => x.Id)
                .Select(x => x.Url)
                .FirstOrDefault();
            await _requestRepository.UpdateAsync(request);
        }

        await _fileStorageService.DeleteIfManagedAsync(existing.Url);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(existing.RequestId, "attachment-removed");
    }

    public override Task<AttachmentDto?> UpdateAsync(int id, AttachmentUpdateDto updateDto)
    {
        throw new NotSupportedException("Attachment URLs can only be managed through SupportRequests uploads and attachment add/remove operations.");
    }
}
