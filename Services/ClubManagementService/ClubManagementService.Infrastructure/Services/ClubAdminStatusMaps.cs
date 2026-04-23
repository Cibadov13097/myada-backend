using ClubManagementService.Domain.Enums;

namespace ClubManagementService.Infrastructure.Services;

internal static class ClubAdminStatusMaps
{
    public static string JobApplicationToApi(JobApplicationStatus status) => status switch
    {
        JobApplicationStatus.Pending => "Pending",
        JobApplicationStatus.Reviewing => "Reviewing",
        JobApplicationStatus.Approved => "Approved",
        JobApplicationStatus.Rejected => "Rejected",
        JobApplicationStatus.Cancelled => "Rejected",
        JobApplicationStatus.InterviewInvited => "InterviewInvited",
        JobApplicationStatus.InterviewScheduled => "InterviewScheduled",
        _ => "Pending"
    };

    public static bool JobMatchesApiFilter(string? statusFilter, JobApplicationStatus entityStatus)
    {
        if (string.IsNullOrWhiteSpace(statusFilter))
            return true;
        return string.Equals(JobApplicationToApi(entityStatus), statusFilter.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static JobApplicationStatus? TryParseJobApiStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        return status.Trim().ToLowerInvariant() switch
        {
            "pending" => JobApplicationStatus.Pending,
            "reviewing" => JobApplicationStatus.Reviewing,
            "approved" => JobApplicationStatus.Approved,
            "rejected" => JobApplicationStatus.Rejected,
            "interviewinvited" or "interview_invited" or "interview-invited" => JobApplicationStatus.InterviewInvited,
            "interviewscheduled" or "interview_scheduled" or "interview-scheduled" => JobApplicationStatus.InterviewScheduled,
            _ => null
        };
    }

    public static bool MembershipMatchesApiFilter(string? statusFilter, string membershipStatus)
    {
        if (string.IsNullOrWhiteSpace(statusFilter))
            return true;
        return string.Equals(NormalizeMembershipStatus(membershipStatus), statusFilter.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeMembershipStatus(string status) => status.Trim() switch
    {
        var s when s.Equals("pending", StringComparison.OrdinalIgnoreCase) => "Pending",
        var s when s.Equals("reviewing", StringComparison.OrdinalIgnoreCase) => "Reviewing",
        var s when s.Equals("approved", StringComparison.OrdinalIgnoreCase) => "Approved",
        var s when s.Equals("rejected", StringComparison.OrdinalIgnoreCase) => "Rejected",
        _ => "Pending"
    };

    public static JobApplicationStatus ParseBulkJobDecision(string decision)
    {
        return decision.Trim().ToLowerInvariant() switch
        {
            "approve" or "approved" => JobApplicationStatus.Approved,
            "reject" or "rejected" => JobApplicationStatus.Rejected,
            "request-changes" or "reviewing" or "changes_requested" => JobApplicationStatus.Reviewing,
            "invite-to-interview" or "invite_to_interview" or "interview_invite" or "interview-invite" => JobApplicationStatus.InterviewInvited,
            _ => JobApplicationStatus.Pending
        };
    }

    public static string? ParseBulkMembershipDecision(string decision)
    {
        return decision.Trim().ToLowerInvariant() switch
        {
            "approve" or "approved" => "Approved",
            "reject" or "rejected" => "Rejected",
            "request-changes" or "reviewing" or "changes_requested" => "Reviewing",
            _ => null
        };
    }
}
