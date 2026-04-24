using AutoMapper;
using ClubManagementService.Application.DTOs.Category;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.DTOs.Subevent;
using ClubManagementService.Domain.Common;
using ClubManagementService.Domain.Entities;

namespace ClubManagementService.Application.Mappings;

internal static class AuditableMappingExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreAuditableBase<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> map)
        where TDestination : AuditableEntity
    {
        return map
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedAt, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedByUserId, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());
    }
}

public class ClubManagementProfile : Profile
{
    public ClubManagementProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>().IgnoreAuditableBase();
        CreateMap<UpdateCategoryDto, Category>().IgnoreAuditableBase();

        CreateMap<Club, ClubDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.FocusAreas, o => o.Ignore())
            .ForMember(d => d.SocialLinks, o => o.Ignore())
            .ForMember(d => d.Resources, o => o.Ignore())
            .ForMember(d => d.Documents, o => o.Ignore())
            .AfterMap((s, d) =>
            {
                d.FocusAreas = ClubFocusAreasJson.ParseForRead(s.FocusAreasJson);
                d.SocialLinks = ClubSocialLinksJson.ParseForRead(s.SocialLinksJson);
                d.Resources = ClubResourcesJson.ParseForRead(s.ResourcesJson);
            });
        CreateMap<CreateClubDto, Club>()
            .IgnoreAuditableBase()
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.FocusAreasJson, o => o.Ignore())
            .ForMember(d => d.SocialLinksJson, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.ResourcesJson, o => o.Ignore())
            .ForMember(d => d.ProfileImageUrl, o => o.Ignore())
            .ForMember(d => d.ProposedProfileImageUrl, o => o.Ignore());
        CreateMap<UpdateClubDto, Club>()
            .IgnoreAuditableBase()
            .ForMember(d => d.ApprovedBy, o => o.Ignore())
            .ForMember(d => d.CategoryId, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.FocusAreasJson, o => o.Ignore())
            .ForMember(d => d.SocialLinksJson, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.ResourcesJson, o => o.Ignore())
            .ForMember(d => d.ProfileImageUrl, o => o.Ignore())
            .ForMember(d => d.ProposedProfileImageUrl, o => o.Ignore());

        CreateMap<Event, EventDto>();
        CreateMap<CreateEventDto, Event>()
            .IgnoreAuditableBase()
            .ForMember(d => d.Status, o => o.Ignore());
        CreateMap<UpdateEventDto, Event>()
            .IgnoreAuditableBase()
            .ForMember(d => d.ClubId, o => o.Ignore())
            .ForMember(d => d.ApprovedBy, o => o.Ignore());

        CreateMap<EventRegistration, EventRegistrationDto>();
        CreateMap<CreateEventRegistrationDto, EventRegistration>()
            .IgnoreAuditableBase()
            .ForMember(d => d.Event, o => o.Ignore())
            .ForMember(d => d.TicketId, o => o.Ignore())
            .ForMember(d => d.RegistrationJwt, o => o.Ignore())
            .ForMember(d => d.EventNameSnapshot, o => o.Ignore())
            .ForMember(d => d.EventImageUrlSnapshot, o => o.Ignore())
            .ForMember(d => d.EventStartTimeSnapshot, o => o.Ignore())
            .ForMember(d => d.EventEndTimeSnapshot, o => o.Ignore())
            .ForMember(d => d.EventLocationSnapshot, o => o.Ignore())
            .ForMember(d => d.EventSeatLimitSnapshot, o => o.Ignore())
            .ForMember(d => d.IsCheckedIn, o => o.Ignore())
            .ForMember(d => d.CheckedInAt, o => o.Ignore())
            .ForMember(d => d.CheckedInBy, o => o.Ignore())
            .ForMember(d => d.ScanCount, o => o.Ignore())
            .ForMember(d => d.LastScannedAt, o => o.Ignore());

        CreateMap<JobVacancy, JobVacancyDto>()
            .ForMember(d => d.ClubName, o => o.MapFrom(s => s.Club != null ? s.Club.Name : null))
            .ForMember(d => d.Title, o => o.MapFrom(s =>
                string.IsNullOrWhiteSpace(s.Title) && s.ClubPosition != null ? s.ClubPosition.Title : s.Title))
            .ForMember(d => d.Status, o => o.MapFrom(s =>
                s.IsDraft ? "draft" : s.IsActive ? "active" : "inactive"))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));
        CreateMap<CreateJobVacancyDto, JobVacancy>()
            .IgnoreAuditableBase()
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.IsDraft, o => o.Ignore())
            .ForMember(d => d.Club, o => o.Ignore())
            .ForMember(d => d.ClubPosition, o => o.Ignore());
        CreateMap<UpdateJobVacancyDto, JobVacancy>()
            .IgnoreAuditableBase()
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.ClubId, o => o.Ignore())
            .ForMember(d => d.PostedBy, o => o.Ignore())
            .ForMember(d => d.Club, o => o.Ignore())
            .ForMember(d => d.ClubPosition, o => o.Ignore());

        CreateMap<JobApplication, JobApplicationDto>();
        CreateMap<CreateJobApplicationDto, JobApplication>()
            .IgnoreAuditableBase()
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.JobVacancy, o => o.Ignore())
            .ForMember(d => d.InterviewSlotId, o => o.Ignore())
            .ForMember(d => d.InterviewSlot, o => o.Ignore());

        CreateMap<Subevent, SubeventDto>();
        CreateMap<CreateSubeventDto, Subevent>().IgnoreAuditableBase();
        CreateMap<UpdateSubeventDto, Subevent>()
            .IgnoreAuditableBase()
            .ForMember(d => d.MainEventId, o => o.Ignore());
    }
}
