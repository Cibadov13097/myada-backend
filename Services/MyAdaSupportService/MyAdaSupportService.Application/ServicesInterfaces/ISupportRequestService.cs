using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface ISupportRequestService
{
    Task<SupportRequestDetailDto> CreateRequestAsync(CreateSupportRequestDto dto, int memberId);

    Task<List<SupportRequestListItemDto>> GetMyRequestsAsync(int memberId);

    Task<List<SupportRequestListItemDto>> GetAssignedRequestsAsync(int staffId);

    Task<List<SupportRequestListItemDto>> GetAllRequestsAsync(RequestFilterDto filter);

    Task<SupportRequestDetailDto> GetRequestDetailAsync(int requestId);

    Task<List<TimelineItemDto>> GetRequestTimelineAsync(int requestId);

    Task CancelRequestAsync(int requestId, int memberId, CancelRequestDto dto);
}
