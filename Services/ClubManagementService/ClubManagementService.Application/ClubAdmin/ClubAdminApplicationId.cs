using System.Globalization;

namespace ClubManagementService.Application.ClubAdmin;

public static class ClubAdminApplicationId
{
    public const string JobPrefix = "job:";
    public const string MembershipPrefix = "membership:";

    public static string Job(int id) => $"{JobPrefix}{id.ToString(CultureInfo.InvariantCulture)}";

    public static string Membership(Guid id) => $"{MembershipPrefix}{id:D}";

    public static bool TryParse(string applicationId, out int jobId, out Guid membershipId)
    {
        jobId = 0;
        membershipId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(applicationId))
            return false;

        var s = applicationId.Trim();
        if (s.StartsWith(JobPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var tail = s[JobPrefix.Length..];
            return int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out jobId) && jobId > 0;
        }

        if (s.StartsWith(MembershipPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var tail = s[MembershipPrefix.Length..];
            return Guid.TryParse(tail, out membershipId) && membershipId != Guid.Empty;
        }

        return false;
    }
}
