using MyAdaSupportService.Application.DTOs.Dispatcher;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface IAssignmentService
{
    Task AssignRequestAsync(int requestId, AssignRequestDto dto, Guid dispatcherId);
    Task ReassignRequestAsync(int requestId, AssignRequestDto dto, Guid dispatcherId);
    Task AddDispatcherInstructionsAsync(int requestId, DispatcherInstructionDto dto, Guid dispatcherId);
}
