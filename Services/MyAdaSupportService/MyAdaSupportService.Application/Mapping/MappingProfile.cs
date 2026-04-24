using AutoMapper;
using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.DTOs.Category;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateSupportRequestDto, Request>()
            .ForMember(dest => dest.Attachments, opt => opt.Ignore());

        CreateMap<CreateAssignedTaskDto, Request>();

        CreateMap<UpdateSupportRequestDto, Request>()
            .ForMember(dest => dest.ImgUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore());

        CreateMap<Request, SupportRequestCreatedDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt,
                opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<Request, SupportRequestListItemDto>()
            .ForMember(dest => dest.Title,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.Service,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Module.ToString() : string.Empty))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.AssignedStaffId,
                opt => opt.MapFrom(src => src.StaffId))
            .ForMember(dest => dest.IsUrgent,
                opt => opt.MapFrom(src => src.IsUrgent));

        CreateMap<Request, SupportRequestDetailDto>()
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.Service,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Module.ToString() : string.Empty))
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.AssignedStaffId,
                opt => opt.MapFrom(src => src.StaffId))
            .ForMember(dest => dest.IsUrgent,
                opt => opt.MapFrom(src => src.IsUrgent))
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom(src => src.Attachments))
            .ForMember(dest => dest.DescriptionFull,
                opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Reporter,
                opt => opt.MapFrom(src => new ReporterInfoDto
                {
                    MemberId = src.CreatedUserId,
                    FullName = "",
                    Email = ""
                }));

        CreateMap<string, Attachment>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src));

        CreateMap<Attachment, AttachmentDto>().ReverseMap();
        CreateMap<Attachment, AttachmentCreateDto>().ReverseMap();
        CreateMap<Attachment, AttachmentUpdateDto>();

        CreateMap<Category, CategoryDto>().ReverseMap();
        CreateMap<Category, CategoryCreateDto>().ReverseMap();
        CreateMap<Category, CategoryUpdateDto>().ReverseMap();

        CreateMap<SupportStaffStatus, SupportStaffStatusDto>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(src => src.ModifiedAt));

        CreateMap<CreateSupportStaffStatusDto, SupportStaffStatus>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => Enum.Parse<StaffAvailabilityStatus>(src.Status, true)));
    }
}
