using AutoMapper;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.Services;

public class SupportStaffStatusService
    : CrudManager<SupportStaffStatus, SupportStaffListItemDto, SupportStaffStatusDto, CreateSupportStaffStatusDto, UpdateSupportStaffStatusDto>,
      ISupportStaffStatusCrudService,
      ISupportStaffStatusService
{
    private readonly ISupportStaffStatusRepository _supportStaffStatusRepository;
    private readonly IMapper _mapper;

    public SupportStaffStatusService(ISupportStaffStatusRepository supportStaffStatusRepository, IMapper mapper)
        : base(supportStaffStatusRepository, mapper)
    {
        _supportStaffStatusRepository = supportStaffStatusRepository;
        _mapper = mapper;
    }

    public async Task<SupportStaffStatusDto?> GetByMemberIdAsync(int memberId)
    {
        var entity = await _supportStaffStatusRepository.GetAsync(x => x.MemberId == memberId);
        return entity == null ? null : _mapper.Map<SupportStaffStatusDto>(entity);
    }

    public async Task<List<SupportStaffStatusDto>> GetByStatusAsync(string status)
    {
        var entities = await _supportStaffStatusRepository.GetAllAsync(x => x.Status.ToString() == status);
        return _mapper.Map<List<SupportStaffStatusDto>>(entities);
    }

    public async Task UpdateStatusAsync(int memberId, UpdateSupportStaffStatusDto dto)
    {
        SupportStaffStatus? entity = null;
        try
        {
            entity = await _supportStaffStatusRepository.GetAsync(x => x.MemberId == memberId);
        }
        catch (KeyNotFoundException)
        {
            entity = null;
        }

        if (!Enum.TryParse<StaffAvailabilityStatus>(dto.Status, true, out var parsedStatus))
            throw new Exception("Invalid support staff status");

        if (entity == null)
        {
            var created = new SupportStaffStatus
            {
                MemberId = memberId,
                Status = parsedStatus
            };
            await _supportStaffStatusRepository.AddAsync(created);
            return;
        }

        entity.Status = parsedStatus;
        await _supportStaffStatusRepository.UpdateAsync(entity);
    }
}
