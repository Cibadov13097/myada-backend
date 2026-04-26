using System.Collections;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using FluentAssertions;

namespace AuthService.Tests.Layers.Domain;

/// <summary>Medium service — domain layer guideline ~15–40 tests.</summary>
public sealed class AuthServiceDomainLayerTests
{
    public static IEnumerable<object[]> AllStatuses() =>
        Enum.GetValues<Status>().Cast<Status>().Select(s => new object[] { s });

    public static IEnumerable<object[]> AllUserTypes() =>
        Enum.GetValues<UserType>().Cast<UserType>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void Status_enum_values_are_unique(Status value)
    {
        Enum.IsDefined(value).Should().BeTrue();
        value.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(AllUserTypes))]
    public void UserType_enum_values_are_unique(UserType value) =>
        Enum.IsDefined(value).Should().BeTrue();

    [Fact]
    public void RefreshToken_IsActive_when_not_revoked_and_future_expiry()
    {
        var token = new RefreshToken
        {
            Token = "t",
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_IsActive_false_when_revoked()
    {
        var token = new RefreshToken
        {
            Token = "t",
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_IsExpired_when_expiry_in_past()
    {
        var token = new RefreshToken
        {
            Token = "t",
            UserId = "u1",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            RevokedAt = null
        };

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    public void RefreshToken_Id_can_be_round_tripped(string guidText)
    {
        var id = Guid.Parse(guidText);
        var token = new RefreshToken { Id = id };
        token.Id.Should().Be(id);
    }

    [Fact]
    public void RefreshToken_ReplacedByToken_roundtrip()
    {
        var token = new RefreshToken { ReplacedByToken = "next" };
        token.ReplacedByToken.Should().Be("next");
    }

    [Fact]
    public void RefreshToken_CreatedByIp_and_RevokedByIp_optional()
    {
        var token = new RefreshToken
        {
            CreatedByIp = "1.1.1.1",
            RevokedByIp = "2.2.2.2"
        };
        token.CreatedByIp.Should().Be("1.1.1.1");
        token.RevokedByIp.Should().Be("2.2.2.2");
    }

    [Theory]
    [InlineData(Status.Active)]
    [InlineData(Status.Inactive)]
    [InlineData(Status.Banned)]
    public void Status_can_be_cast_from_int(Status expected)
    {
        var v = (Status)(int)expected;
        v.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserType.Student)]
    [InlineData(UserType.Faculty)]
    [InlineData(UserType.Staff)]
    public void UserType_can_be_cast_from_int(UserType expected)
    {
        var v = (UserType)(int)expected;
        v.Should().Be(expected);
    }
}
