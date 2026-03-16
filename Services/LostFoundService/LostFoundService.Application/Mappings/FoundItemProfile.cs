using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Mappings
{
    public class FoundItemProfile : Profile
    {
        public FoundItemProfile()
        {
            CreateMap<FoundItem, GetFoundItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
            CreateMap<GetFoundItemDto, FoundItem>();
            CreateMap<AddFoundItemDto, FoundItem>()
                .ForMember(d => d.BuildingId, o => o.MapFrom(s => s.BuildingId))
                .ForMember(d => d.RoomId, o => o.MapFrom(s => s.RoomId));
            CreateMap<UpdateFoundItemDto, FoundItem>()
                .ForMember(d => d.CategoryId, o => o.Condition(s => s.CategoryId.HasValue))
                .ForMember(d => d.BuildingId, o => o.Condition(s => s.BuildingId.HasValue))
                .ForMember(d => d.RoomId, o => o.Condition(s => s.RoomId.HasValue));

            CreateMap<AdminCreateFoundItem, FoundItem>()
                .ForMember(d => d.BuildingId, o => o.MapFrom(s => s.BuildingId))
                .ForMember(d => d.RoomId, o => o.MapFrom(s => s.RoomId));
            CreateMap<FoundItem, AdminFoundItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
            CreateMap<AdminFoundItemDto, FoundItem>()
                .ForMember(d => d.BuildingId, o => o.MapFrom(s => s.BuildingId))
                .ForMember(d => d.RoomId, o => o.MapFrom(s => s.RoomId));
        }
    }
}
