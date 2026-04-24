using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface ISupportRequestService
{
    Task<SupportRequestDetailDto> CreateRequestAsync(CreateSupportRequestDto dto, Guid memberId);

    Task<List<SupportRequestListItemDto>> GetMyRequestsAsync(Guid memberId);

    Task<List<SupportRequestListItemDto>> GetAssignedRequestsAsync(Guid staffId);

    Task<List<SupportRequestListItemDto>> GetAllRequestsAsync(RequestFilterDto filter);

    Task<SupportRequestDetailDto> GetRequestDetailAsync(int requestId);

    Task<List<TimelineItemDto>> GetRequestTimelineAsync(int requestId);

    Task CancelRequestAsync(int requestId, Guid memberId, CancelRequestDto dto);
}
