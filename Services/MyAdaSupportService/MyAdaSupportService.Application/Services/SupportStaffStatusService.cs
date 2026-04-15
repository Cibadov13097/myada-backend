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

    public async Task<SupportStaffStatusDto> CreateStatusAsync(CreateSupportStaffStatusDto dto)
    {
        var existing = await _supportStaffStatusRepository.GetByMemberIdAsync(dto.MemberId);
        if (existing != null)
            throw new Exception("Support staff status already exists for this member");

        if (!Enum.TryParse<StaffAvailabilityStatus>(dto.Status, true, out var parsedStatus))
            throw new Exception("Invalid support staff status");

        var created = await _supportStaffStatusRepository.AddAsync(new SupportStaffStatus
        {
            MemberId = dto.MemberId,
            Status = parsedStatus
        });

        return _mapper.Map<SupportStaffStatusDto>(created);
    }

    public async Task<SupportStaffStatusDto> GetOrCreateByMemberIdAsync(Guid memberId)
    {
        return await EnsureStatusAsync(memberId, nameof(StaffAvailabilityStatus.Offline));
    }

    public async Task<SupportStaffStatusDto> EnsureStatusAsync(Guid memberId, string? status = null)
    {
        var parsedStatus = StaffAvailabilityStatus.Offline;
        if (!string.IsNullOrWhiteSpace(status) &&
            !Enum.TryParse<StaffAvailabilityStatus>(status, true, out parsedStatus))
        {
            throw new Exception("Invalid support staff status");
        }

        var entity = await _supportStaffStatusRepository.GetByMemberIdAsync(memberId);
        if (entity != null)
            return _mapper.Map<SupportStaffStatusDto>(entity);

        var created = await _supportStaffStatusRepository.AddAsync(new SupportStaffStatus
        {
            MemberId = memberId,
            Status = parsedStatus
        });

        return _mapper.Map<SupportStaffStatusDto>(created);
    }

    public async Task<SupportStaffStatusDto?> GetByMemberIdAsync(Guid memberId)
    {
        var entity = await _supportStaffStatusRepository.GetByMemberIdAsync(memberId);
        return entity == null ? null : _mapper.Map<SupportStaffStatusDto>(entity);
    }

    public async Task<List<SupportStaffStatusDto>> GetByStatusAsync(string status)
    {
        var entities = await _supportStaffStatusRepository.GetAllAsync(x => x.Status.ToString() == status);
        return _mapper.Map<List<SupportStaffStatusDto>>(entities);
    }

    public async Task UpdateStatusAsync(Guid memberId, UpdateSupportStaffStatusDto dto)
    {
        var entity = await _supportStaffStatusRepository.GetByMemberIdAsync(memberId);

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
