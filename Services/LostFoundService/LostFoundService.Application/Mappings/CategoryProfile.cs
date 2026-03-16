using AutoMapper;
using LostFoundService.Application.DTOs.Category;
using LostFoundService.Domain.Entities;

namespace LostFoundService.Application.Mappings
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDto>().ReverseMap();
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();
        }
    }
}

