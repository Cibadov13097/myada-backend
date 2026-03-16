using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Mappings
{
    public class LostItemProfile : Profile
    {
        public LostItemProfile()
        {
            CreateMap<LostItem, GetLostItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
            CreateMap<LostItem, AdminLostItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));

            CreateMap<AddLostItemDto, LostItem>()
                .ForMember(d => d.BuildingId, o => o.MapFrom(s => s.BuildingId))
                .ForMember(d => d.RoomId, o => o.MapFrom(s => s.RoomId));
            CreateMap<UpdateLostItemDto, LostItem>()
                .ForMember(d => d.CategoryId, o => o.Condition(s => s.CategoryId.HasValue))
                .ForMember(d => d.BuildingId, o => o.Condition(s => s.BuildingId.HasValue))
                .ForMember(d => d.RoomId, o => o.Condition(s => s.RoomId.HasValue));
        }
    }
}
