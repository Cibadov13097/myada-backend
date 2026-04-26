using System.Collections.Generic;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Services;
using FluentAssertions;

namespace ClubManagementService.Tests.Layers.Infrastructure;

/// <summary>Large service — infrastructure layer guideline ~20–50 tests (status mapping helpers).</summary>
public sealed class ClubAdminStatusMapsLayerTests
{
    public static IEnumerable<object[]> JobStatuses() =>
        Enum.GetValues<JobApplicationStatus>().Cast<JobApplicationStatus>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(JobStatuses))]
    public void JobApplicationToApi_returns_non_empty(JobApplicationStatus status)
    {
        var api = ClubAdminStatusMaps.JobApplicationToApi(status);
        api.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null, JobApplicationStatus.Pending, true)]
    [InlineData("", JobApplicationStatus.Rejected, true)]
    [InlineData("  ", JobApplicationStatus.Approved, true)]
    [InlineData("Approved", JobApplicationStatus.Approved, true)]
    [InlineData("rejected", JobApplicationStatus.Rejected, true)]
    [InlineData("InterviewInvited", JobApplicationStatus.InterviewInvited, true)]
    public void JobMatchesApiFilter_respects_rules(string? filter, JobApplicationStatus entity, bool expected)
    {
        ClubAdminStatusMaps.JobMatchesApiFilter(filter, entity).Should().Be(expected);
    }

    [Theory]
    [InlineData("pending", JobApplicationStatus.Pending)]
    [InlineData("REVIEWING", JobApplicationStatus.Reviewing)]
    [InlineData("interview-invited", JobApplicationStatus.InterviewInvited)]
    public void TryParseJobApiStatus_parses_known_strings(string raw, JobApplicationStatus expected)
    {
        var parsed = ClubAdminStatusMaps.TryParseJobApiStatus(raw);
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("nope")]
    public void TryParseJobApiStatus_unknown_returns_null(string raw) =>
        ClubAdminStatusMaps.TryParseJobApiStatus(raw).Should().BeNull();

    [Theory]
    [InlineData(null, "Pending", true)]
    [InlineData("approved", "approved", true)]
    [InlineData("Rejected", "rejected", true)]
    public void MembershipMatchesApiFilter_handles_filters(string? filter, string entityStatus, bool expected)
    {
        ClubAdminStatusMaps.MembershipMatchesApiFilter(filter, entityStatus).Should().Be(expected);
    }

    [Theory]
    [InlineData(" pending ", "Pending")]
    [InlineData("APPROVED", "Approved")]
    public void NormalizeMembershipStatus_canonicalizes(string raw, string expected) =>
        ClubAdminStatusMaps.NormalizeMembershipStatus(raw).Should().Be(expected);

    [Theory]
    [InlineData("approve", JobApplicationStatus.Approved)]
    [InlineData("REJECT", JobApplicationStatus.Rejected)]
    [InlineData("invite-to-interview", JobApplicationStatus.InterviewInvited)]
    [InlineData("nope", JobApplicationStatus.Pending)]
    public void ParseBulkJobDecision_maps_strings(string raw, JobApplicationStatus expected) =>
        ClubAdminStatusMaps.ParseBulkJobDecision(raw).Should().Be(expected);

    [Theory]
    [InlineData("approve", "Approved")]
    [InlineData("reject", "Rejected")]
    [InlineData("unknown", null)]
    public void ParseBulkMembershipDecision_maps_strings(string raw, string? expected) =>
        ClubAdminStatusMaps.ParseBulkMembershipDecision(raw).Should().Be(expected);
}
