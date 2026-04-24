using AutoMapper;
using LostFoundService.Api.Models.LostAndFound.Requests;
using LostFoundService.Api.Models.LostAndFound.Responses;
using LostFoundService.Application.DTOs.LostAndFound;
using LostFoundService.Domain.Entities;
using LostFoundService.Domain.Enums;
using System.Globalization;

namespace LostFoundService.Api.Mappings;

public class LostAndFoundApiProfile : Profile
{
    public LostAndFoundApiProfile()
    {
        CreateMap<ManualCreateItemRequest, CreateLostFoundItemCommand>()
            .ForMember(d => d.ImageUrls, o => o.Ignore())
            .ForMember(d => d.IntakePhotoUrl, o => o.Ignore())
            .ForMember(d => d.HandoverProofUrl, o => o.Ignore())
            .ForMember(d => d.PostedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.IsAdminCreate, o => o.Ignore());

        CreateMap<UserLostReportRequest, CreateLostFoundItemCommand>()
            .ForMember(d => d.ImageUrls, o => o.Ignore())
            .ForMember(d => d.IntakePhotoUrl, o => o.Ignore())
            .ForMember(d => d.HandoverProofUrl, o => o.Ignore())
            .ForMember(d => d.PostedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.IsAdminCreate, o => o.Ignore());

        CreateMap<ReportRequest, CreateLostFoundItemCommand>()
            .ForMember(d => d.ImageUrls, o => o.Ignore())
            .ForMember(d => d.IntakePhotoUrl, o => o.Ignore())
            .ForMember(d => d.HandoverProofUrl, o => o.Ignore())
            .ForMember(d => d.PostedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.IsAdminCreate, o => o.Ignore());

        CreateMap<PatchItemRequest, UpdateLostFoundItemCommand>()
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore());

        CreateMap<NotifyRequest, NotifyLostFoundOwnerCommand>()
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore());

        CreateMap<CreateClaimRequest, CreateLostFoundClaimCommand>()
            .ForMember(d => d.UserId, o => o.Ignore());

        CreateMap<ConfirmReceiptRequest, ConfirmLostFoundReceiptCommand>()
            .ForMember(d => d.IntakePhotoUrl, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore());

        CreateMap<CompleteDeliveryRequest, CompleteLostFoundDeliveryCommand>()
            .ForMember(d => d.HandoverProofUrl, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore());

        CreateMap<LostFoundStats, StatsResponse>();

        CreateMap<LostFoundItemClaim, ItemClaimResponse>()
            .ForMember(d => d.ClaimType, o => o.MapFrom(s => ToClaimTypeString(s.ClaimType)))
            .ForMember(d => d.Status, o => o.MapFrom(s => ToClaimStatusString(s.Status)));

        CreateMap<LostFoundItem, LostFoundItemResponse>()
            .ForMember(d => d.Type, o => o.MapFrom(s => ToItemTypeString(s.Type)))
            .ForMember(d => d.Status, o => o.MapFrom(s => ToPublicStatusString(s.Status)))
            .ForMember(d => d.AdminStatus, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? ToAdminStatusString(s.AdminStatus) : null))
            .ForMember(d => d.DatePosted, o => o.MapFrom(s => s.PostedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(d => d.DaysAgo, o => o.MapFrom(s => Math.Max((int)(DateTime.UtcNow.Date - s.PostedAt.Date).TotalDays, 0)))
            .ForMember(d => d.Image, o => o.MapFrom(s => s.PrimaryImageUrl ?? s.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault()))
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.OrderBy(x => x.SortOrder).Select(x => x.Url).ToList()))
            .ForMember(d => d.DateLost, o => o.MapFrom(s => s.DateLost.HasValue
                ? s.DateLost.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null))
            .ForMember(d => d.Condition, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? ToConditionString(s.Condition) : null))
            .ForMember(d => d.StorageBinId, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.StorageBinId : null))
            .ForMember(d => d.AdminNotes, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.AdminNotes : null))
            .ForMember(d => d.ReportedColor, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.ReportedColor : null))
            .ForMember(d => d.IntakePhotoUrl, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.IntakePhotoUrl : null))
            .ForMember(d => d.ConfirmAccuracy, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.ConfirmAccuracy : null))
            .ForMember(d => d.ClaimantName, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.ClaimantName : null))
            .ForMember(d => d.ClaimantStudentId, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.ClaimantStudentId : null))
            .ForMember(d => d.StudentIdVerified, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.StudentIdVerified : null))
            .ForMember(d => d.HandoverProofUrl, o => o.MapFrom((s, _, _, context) =>
                IncludeAdminFields(context) ? s.HandoverProofUrl : null));
    }

    private static bool IncludeAdminFields(ResolutionContext context)
    {
        return context.Items.TryGetValue("IncludeAdminFields", out var includeAdminFields) &&
               includeAdminFields is bool value &&
               value;
    }

    private static string ToItemTypeString(LostFoundItemType type) => type == LostFoundItemType.Found ? "found" : "lost";

    private static string ToPublicStatusString(LostFoundPublicStatus status) =>
        status == LostFoundPublicStatus.Active ? "Active" : "Pending Verification";

    private static string? ToAdminStatusString(LostFoundAdminStatus? status) => status?.ToString();

    private static string? ToConditionString(LostFoundItemCondition? condition)
    {
        return condition switch
        {
            LostFoundItemCondition.Good => "good",
            LostFoundItemCondition.Fair => "fair",
            LostFoundItemCondition.Damaged => "damaged",
            _ => null
        };
    }

    private static string ToClaimTypeString(LostFoundClaimType claimType) =>
        claimType == LostFoundClaimType.Owner ? "owner" : "finder";

    private static string ToClaimStatusString(LostFoundClaimStatus status)
    {
        return status switch
        {
            LostFoundClaimStatus.PendingReview => "pending_review",
            LostFoundClaimStatus.Approved => "approved",
            LostFoundClaimStatus.Rejected => "rejected",
            _ => "pending_review"
        };
    }
}
