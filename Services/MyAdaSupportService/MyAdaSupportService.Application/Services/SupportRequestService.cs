using AutoMapper;
using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;
using Shared.Contracts.Events.Notifications;

namespace MyAdaSupportService.Application.Services;

public class SupportRequestService
    : CrudManager<Request, SupportRequestListItemDto, SupportRequestDetailDto, CreateSupportRequestDto, UpdateSupportRequestDto>,
      IRequestCrudService,
      ISupportRequestService,
      IRequestStatusService,
      IAssignmentService
{
    private readonly IRequestRepository _requestRepository;
    private readonly IMapper _mapper;
    private readonly ILocationService _locationService;
    private readonly ISupportRealtimeNotifier _realtimeNotifier;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISupportNotificationPublisher _notificationPublisher;

    public SupportRequestService(
        IRequestRepository requestRepository,
        IMapper mapper,
        ILocationService locationService,
        ISupportRealtimeNotifier realtimeNotifier,
        IFileStorageService fileStorageService,
        ISupportNotificationPublisher notificationPublisher)
        : base(requestRepository, mapper)
    {
        _requestRepository = requestRepository;
        _mapper = mapper;
        _locationService = locationService;
        _realtimeNotifier = realtimeNotifier;
        _fileStorageService = fileStorageService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<SupportRequestDetailDto> CreateRequestAsync(CreateSupportRequestDto dto, Guid memberId)
    {
        var entity = _mapper.Map<Request>(dto);
        entity.CreatedUserId = memberId;
        entity.Status = RequestStatus.New;
        ApplyAttachmentUrls(entity, dto.AttachmentUrls, memberId);

        var created = await _requestRepository.AddAsync(entity);

        var result = _mapper.Map<SupportRequestDetailDto>(created);
        result.Location = await ResolveLocationAsync(created);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(created.Id, "created");

        if (created.StaffId is Guid assignedStaffId && assignedStaffId != Guid.Empty)
            await PublishTaskAssignedNotificationAsync(created, assignedStaffId, memberId);

        return result;
    }

    public async Task<List<SupportRequestListItemDto>> GetMyRequestsAsync(Guid memberId)
    {
        var requests = await _requestRepository.GetRequestsByMemberIdAsync(memberId);
        return await MapListDtosWithLocationAsync(requests.OrderByDescending(x => x.CreatedAt).ToList());
    }

    public async Task<List<SupportRequestListItemDto>> GetAssignedRequestsAsync(Guid staffId)
    {
        var requests = await _requestRepository.GetRequestsByStaffIdAsync(staffId);
        return await MapListDtosWithLocationAsync(requests);
    }

    public async Task<List<SupportRequestListItemDto>> GetAllRequestsAsync(RequestFilterDto filter)
    {
        RequestStatus? statusFilter = null;
        var includeOpenStatuses = false;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (filter.Status.Equals("open", StringComparison.OrdinalIgnoreCase))
            {
                includeOpenStatuses = true;
            }
            else if (Enum.TryParse<RequestStatus>(filter.Status, true, out var parsed))
            {
                statusFilter = parsed;
            }
        }

        var requests = await _requestRepository.GetAllWithDetailsAsync(
            x =>
                (!filter.MemberId.HasValue || x.CreatedUserId == filter.MemberId.Value) &&
                (!filter.StaffId.HasValue || x.StaffId == filter.StaffId.Value) &&
                (filter.CategoryId == null || x.CategoryId == filter.CategoryId) &&
                (includeOpenStatuses
                    ? (x.Status != RequestStatus.Completed && x.Status != RequestStatus.Cancelled)
                    : (statusFilter == null || x.Status == statusFilter.Value)) &&
                (string.IsNullOrWhiteSpace(filter.Service) || (x.Category != null && x.Category.Module.ToString() == filter.Service)));

        return await MapListDtosWithLocationAsync(requests);
    }

    public async Task<SupportRequestDetailDto> GetRequestDetailAsync(int requestId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        return await MapDetailDtoWithLocationAsync(request);
    }

    public async Task<List<TimelineItemDto>> GetRequestTimelineAsync(int requestId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        return BuildTimeline(request);
    }

    public async Task CancelRequestAsync(int requestId, Guid memberId, CancelRequestDto dto)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        if (request.CreatedUserId != memberId)
            throw new Exception("You can only cancel your own requests");

        request.Status = RequestStatus.Cancelled;
        request.CancelReason = dto.Reason;
        request.CompletedAt = null;

        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "cancelled");
        await PublishCancellationNotificationsAsync(request);
    }

    public async Task AssignRequestAsync(int requestId, AssignRequestDto dto, Guid dispatcherId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        request.StaffId = dto.StaffId;
        request.Status = RequestStatus.Assigned;
        request.AssignedBy = dispatcherId;
        request.AssignedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.DispatcherInstructions))
        {
            request.DispatcherInstructions = dto.DispatcherInstructions;
        }

        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "assigned");
        await PublishTaskAssignedNotificationAsync(request, dto.StaffId, dispatcherId);
    }

    public async Task ReassignRequestAsync(int requestId, AssignRequestDto dto, Guid dispatcherId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        request.StaffId = dto.StaffId;
        request.Status = RequestStatus.Assigned;
        request.AssignedBy = dispatcherId;
        request.AssignedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.DispatcherInstructions))
        {
            request.DispatcherInstructions = dto.DispatcherInstructions;
        }

        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "reassigned");
        await PublishTaskAssignedNotificationAsync(request, dto.StaffId, dispatcherId);
    }

    public async Task AddDispatcherInstructionsAsync(int requestId, DispatcherInstructionDto dto, Guid dispatcherId)
    {
        var request = await _requestRepository.GetByIdOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        if (!string.IsNullOrWhiteSpace(dto.DispatcherInstructions))
        {
            request.DispatcherInstructions = dto.DispatcherInstructions;
        }

        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "dispatcher-instructions-updated");
    }

    public async Task MarkAsStartedAsync(int requestId, Guid staffId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        if (request.StaffId != staffId)
            throw new Exception("Only assigned staff can start this request");

        request.Status = RequestStatus.InProgress;
        request.StartedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "started");
        await PublishStatusChangedNotificationAsync(request, staffId);
    }

    public async Task MarkAsInProgressAsync(int requestId, Guid staffId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        if (request.StaffId != staffId)
            throw new Exception("Only assigned staff can update this request");

        request.Status = RequestStatus.InProgress;
        request.StartedAt ??= DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "in-progress");
        await PublishStatusChangedNotificationAsync(request, staffId);
    }

    public async Task MarkAsCompletedAsync(int requestId, Guid staffId)
    {
        var request = await _requestRepository.GetByIdWithDetailsOrDefaultAsync(requestId);

        if (request == null)
            throw new Exception("Request not found");

        if (request.StaffId != staffId)
            throw new Exception("Only assigned staff can complete this request");

        request.Status = RequestStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request);
        await _realtimeNotifier.NotifySupportRequestChangedAsync(request.Id, "completed");
        await PublishStatusChangedNotificationAsync(request, staffId);
    }

    private async Task<List<SupportRequestListItemDto>> MapListDtosWithLocationAsync(List<Request> requests)
    {
        var result = new List<SupportRequestListItemDto>();

        foreach (var request in requests)
        {
            var dto = _mapper.Map<SupportRequestListItemDto>(request);
            dto.Location = await ResolveLocationAsync(request);
            result.Add(dto);
        }

        return result;
    }

    private async Task<SupportRequestDetailDto> MapDetailDtoWithLocationAsync(Request request)
    {
        var dto = _mapper.Map<SupportRequestDetailDto>(request);
        dto.Location = await ResolveLocationAsync(request);
        dto.Timeline = BuildTimeline(request);
        return dto;
    }

    private static List<TimelineItemDto> BuildTimeline(Request request)
    {
        var timeline = new List<TimelineItemDto>
        {
            new()
            {
                Step = "Created",
                Detail = "Request created.",
                Done = true,
                CreatedAt = request.CreatedAt
            }
        };

        if (request.AssignedAt.HasValue)
        {
            timeline.Add(new TimelineItemDto
            {
                Step = "Assigned",
                Detail = request.StaffId.HasValue
                    ? $"Assigned to staff member {request.StaffId.Value}."
                    : "Assigned by dispatcher.",
                Done = true,
                CreatedAt = request.AssignedAt.Value
            });
        }

        if (!string.IsNullOrWhiteSpace(request.DispatcherInstructions) && request.AssignedAt.HasValue)
        {
            timeline.Add(new TimelineItemDto
            {
                Step = "DispatcherInstructions",
                Detail = request.DispatcherInstructions,
                Done = true,
                CreatedAt = request.AssignedAt.Value
            });
        }

        if (request.StartedAt.HasValue)
        {
            timeline.Add(new TimelineItemDto
            {
                Step = "InProgress",
                Detail = "Work started by assigned staff.",
                Done = true,
                CreatedAt = request.StartedAt.Value
            });
        }

        if (request.CompletedAt.HasValue && request.Status == RequestStatus.Completed)
        {
            timeline.Add(new TimelineItemDto
            {
                Step = "Completed",
                Detail = "Request completed.",
                Done = true,
                CreatedAt = request.CompletedAt.Value
            });
        }

        if (request.Status == RequestStatus.Cancelled)
        {
            timeline.Add(new TimelineItemDto
            {
                Step = "Cancelled",
                Detail = string.IsNullOrWhiteSpace(request.CancelReason)
                    ? "Request cancelled."
                    : $"Request cancelled: {request.CancelReason}",
                Done = true,
                CreatedAt = request.ModifiedAt ?? DateTime.UtcNow
            });
        }

        return timeline.OrderBy(x => x.CreatedAt).ToList();
    }

    private async Task<string> ResolveLocationAsync(Request request)
    {
        return await _locationService.ResolveLocationAsync(
            request.IsBuilding,
            request.BuildingId,
            request.IsRoom,
            request.RoomId,
            request.AreaDetails);
    }

    private void ApplyAttachmentUrls(Request entity, IEnumerable<string>? attachmentUrls, Guid userId)
    {
        var normalizedUrls = NormalizeAttachmentUrls(attachmentUrls);
        if (normalizedUrls.Count == 0)
        {
            entity.ImgUrl = null;
            entity.Attachments = new List<Attachment>();
            return;
        }

        var unsupportedUrl = normalizedUrls.FirstOrDefault(url => !_fileStorageService.IsManagedImageUrl(url));
        if (unsupportedUrl != null)
        {
            throw new InvalidOperationException("Attachment URLs must be uploaded image URLs from SupportRequests uploads.");
        }

        entity.ImgUrl = normalizedUrls[0];
        entity.Attachments = normalizedUrls
            .Select(url =>
            {
                var attachment = new Attachment { Url = url };
                attachment.SetCreated(userId);
                return attachment;
            })
            .ToList();
    }

    private static List<string> NormalizeAttachmentUrls(IEnumerable<string>? attachmentUrls)
    {
        return attachmentUrls?
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? new List<string>();
    }

    private async Task PublishTaskAssignedNotificationAsync(Request request, Guid assignedStaffId, Guid actorUserId)
    {
        if (assignedStaffId == Guid.Empty)
            return;

        await _notificationPublisher.PublishAsync(new NotificationQueueMessage
        {
            Type = "New Task Assigned",
            Message = BuildRequestDisplayName(request),
            Channel = "Push",
            Source = "myada-support-service",
            Action = "support.request.assigned",
            EntityType = "support_request",
            EntityId = request.Id.ToString(),
            ActorUserId = actorUserId,
            RecipientUserId = assignedStaffId
        });
    }

    private async Task PublishCancellationNotificationsAsync(Request request)
    {
        var requestName = BuildRequestDisplayName(request);
        var recipients = new HashSet<Guid>();

        if (request.CreatedUserId != Guid.Empty)
            recipients.Add(request.CreatedUserId);

        if (request.StaffId.HasValue && request.StaffId.Value != Guid.Empty)
            recipients.Add(request.StaffId.Value);

        foreach (var recipient in recipients)
        {
            await _notificationPublisher.PublishAsync(new NotificationQueueMessage
            {
                Type = "Request Canceled",
                Message = requestName,
                Channel = "Push",
                Source = "myada-support-service",
                Action = "support.request.cancelled",
                EntityType = "support_request",
                EntityId = request.Id.ToString(),
                ActorUserId = request.CreatedUserId,
                RecipientUserId = recipient
            });
        }
    }

    private async Task PublishStatusChangedNotificationAsync(Request request, Guid actorUserId)
    {
        if (request.CreatedUserId == Guid.Empty)
            return;

        await _notificationPublisher.PublishAsync(new NotificationQueueMessage
        {
            Type = BuildRequestDisplayName(request),
            Message = FormatRequestStatus(request.Status),
            Channel = "Push",
            Source = "myada-support-service",
            Action = "support.request.status_changed",
            EntityType = "support_request",
            EntityId = request.Id.ToString(),
            ActorUserId = actorUserId,
            RecipientUserId = request.CreatedUserId
        });
    }

    private static string BuildRequestDisplayName(Request request)
    {
        if (!string.IsNullOrWhiteSpace(request.Category?.Name))
            return request.Category.Name.Trim();

        return $"Request #{request.Id}";
    }

    private static string FormatRequestStatus(RequestStatus status) => status switch
    {
        RequestStatus.New => "New",
        RequestStatus.Assigned => "Assigned",
        RequestStatus.InProgress => "In Progress",
        RequestStatus.Completed => "Completed",
        RequestStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
