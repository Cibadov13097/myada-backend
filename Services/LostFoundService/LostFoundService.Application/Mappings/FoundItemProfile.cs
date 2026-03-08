using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Application.DTOs.Location;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Mappings
{
    public class FoundItemProfile : Profile
    {
        public FoundItemProfile()
        {
            CreateMap<Location, LocationDto>();
            CreateMap<FoundItem, GetFoundItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId))
                .ForMember(d => d.Location, o => o.MapFrom(s => s.LocationFound))
                .ForMember(d => d.LocationFound, o => o.MapFrom(s => FormatLocation(s.LocationFound)));
            CreateMap<GetFoundItemDto, FoundItem>();
            CreateMap<AddFoundItemDto, FoundItem>()
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId));
            CreateMap<UpdateFoundItemDto, FoundItem>()
                .ForMember(d => d.CategoryId, o => o.Condition(s => s.CategoryId.HasValue))
                .ForMember(d => d.LocationId, o => o.Condition(s => s.LocationId.HasValue));

            CreateMap<AdminCreateFoundItem, FoundItem>()
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId));
            CreateMap<FoundItem, AdminFoundItemDto>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
            CreateMap<AdminFoundItemDto, FoundItem>()
                .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId));
        }

        private static string FormatLocation(Location? loc)
        {
            if (loc == null) return string.Empty;
            var parts = new[] { loc.Place, loc.Floor, loc.Room }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(", ", parts);
        }
    }
}
