using AutoMapper;
using LostFoundService.Application.DTOs.LostItem;
using LostFoundService.Application.DTOs.LostItem.Admin;
using LostFoundService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Application.Mappings
{
  

    public class LostItemProfile : Profile
    {
        public LostItemProfile()
        {
            CreateMap<LostItem, GetLostItemDto>();
            CreateMap<LostItem, AdminLostItemDto>();

            CreateMap<AddLostItemDto, LostItem>();
            CreateMap<UpdateLostItemDto, LostItem>();
        }
    }

}
