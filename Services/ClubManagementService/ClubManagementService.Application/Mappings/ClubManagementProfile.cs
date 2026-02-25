using AutoMapper;
using ClubManagementService.Application.DTOs.Category;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.DTOs.Subevent;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Mappings;

public class ClubManagementProfile : Profile
{
    public ClubManagementProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        CreateMap<Club, ClubDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
        CreateMap<CreateClubDto, Club>();
        CreateMap<UpdateClubDto, Club>();

        CreateMap<Event, EventDto>();
        CreateMap<CreateEventDto, Event>();
        CreateMap<UpdateEventDto, Event>();

        CreateMap<EventRegistration, EventRegistrationDto>();
        CreateMap<CreateEventRegistrationDto, EventRegistration>();

        CreateMap<JobVacancy, JobVacancyDto>();
        CreateMap<CreateJobVacancyDto, JobVacancy>();
        CreateMap<UpdateJobVacancyDto, JobVacancy>();

        CreateMap<JobApplication, JobApplicationDto>();
        CreateMap<CreateJobApplicationDto, JobApplication>();

        CreateMap<Subevent, SubeventDto>();
        CreateMap<CreateSubeventDto, Subevent>();
        CreateMap<UpdateSubeventDto, Subevent>();
    }
}
