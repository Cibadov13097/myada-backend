using MyAdaSupportService.Application.DTOs.Dispatcher;

namespace MyAdaSupportService.Application.ServicesInterfaces;

public interface IAssignmentService
{
    Task AssignRequestAsync(int requestId, AssignRequestDto dto, int dispatcherId);
    Task ReassignRequestAsync(int requestId, AssignRequestDto dto, int dispatcherId);
    Task AddDispatcherInstructionsAsync(int requestId, DispatcherInstructionDto dto, int dispatcherId);
}
