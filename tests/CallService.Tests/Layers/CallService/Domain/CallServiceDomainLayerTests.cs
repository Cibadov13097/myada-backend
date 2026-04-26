using System.Collections;
using System.Collections.Generic;
using CallService.Api.Realtime;
using FluentAssertions;

namespace CallService.Tests.Layers.Domain;

/// <summary>Small service — domain / core layer guideline ~5–15 tests.</summary>
public sealed class CallServiceDomainLayerTests
{
    public static IEnumerable<object[]> AllStatuses() =>
        Enum.GetValues<CallRequestStatus>().Cast<CallRequestStatus>().Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void CallRequestStatus_values_are_defined(CallRequestStatus status) =>
        Enum.IsDefined(status).Should().BeTrue();

    [Fact]
    public void CallRequestState_defaults_to_pending()
    {
        var state = new CallRequestState
        {
            CallId = "c1",
            RoomId = "r1",
            CallerUserId = "u1",
            CallerConnectionId = "conn1",
            CallerDisplayName = "Alice",
            DispatcherUserId = "d1",
            RequestedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        state.Status.Should().Be(CallRequestStatus.Pending);
    }

    [Fact]
    public void TurnOptions_defaults_are_sane()
    {
        var t = new TurnOptions();
        t.Enabled.Should().BeFalse();
        t.Urls.Should().NotBeNull();
        t.CredentialTtlSeconds.Should().Be(3600);
    }
}
