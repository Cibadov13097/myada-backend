using AutoMapper;
using LostFoundService.Application.DTOs.FoundItem;
using LostFoundService.Application.DTOs.FoundItem.Admin;
using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Mappings
{
    public class FoundItemProfile:Profile
    {
        public FoundItemProfile()
        {
            CreateMap<GetFoundItemDto,FoundItem>().ReverseMap();
            CreateMap<AddFoundItemDto, FoundItem>();
            CreateMap<UpdateFoundItemDto, FoundItem>();

            CreateMap<AdminCreateFoundItem, FoundItem>();
        CreateMap<AdminFoundItemDto,FoundItem>().ReverseMap();
        }
    }
}
