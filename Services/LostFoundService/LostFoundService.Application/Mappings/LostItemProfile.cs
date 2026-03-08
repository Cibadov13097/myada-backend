using AutoMapper;
using LostFoundService.Application.DTOs.Location;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Mappings
{
    public class LostItemProfile : Profile
    {
        public LostItemProfile()
        {
            CreateMap<Location, LocationDto>();
            CreateMap<LostItem, GetLostItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId))
                .ForMember(d => d.Location, o => o.MapFrom(s => s.LocationLost))
                .ForMember(d => d.LocationLost, o => o.MapFrom(s => FormatLocation(s.LocationLost)));
            CreateMap<LostItem, AdminLostItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId))
                .ForMember(d => d.Location, o => o.MapFrom(s => s.LocationLost))
                .ForMember(d => d.LocationLost, o => o.MapFrom(s => FormatLocation(s.LocationLost)));

            CreateMap<AddLostItemDto, LostItem>()
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId));
            CreateMap<UpdateLostItemDto, LostItem>()
                .ForMember(d => d.CategoryId, o => o.Condition(s => s.CategoryId.HasValue))
                .ForMember(d => d.LocationId, o => o.Condition(s => s.LocationId.HasValue));
        }

        private static string FormatLocation(Location? loc)
        {
            if (loc == null) return string.Empty;
            var parts = new[] { loc.Place, loc.Floor, loc.Room }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(", ", parts);
        }
    }
}
