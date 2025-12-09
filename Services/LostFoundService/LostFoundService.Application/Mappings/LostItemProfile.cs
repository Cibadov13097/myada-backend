using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Mappings
{
    public class LostItemProfile:Profile
    {
        public LostItemProfile()
        {
            CreateMap<GetLostItemDto, LostItem>().ReverseMap();
            CreateMap<AddLostItemDto, LostItem>();
            CreateMap<UpdateLostItemDto, LostItem>();
        }
    }
}
